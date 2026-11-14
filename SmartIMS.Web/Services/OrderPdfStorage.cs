namespace SmartIMS.Web.Services;

public sealed class OrderPdfStorage(SystemSettingsService settings, IWebHostEnvironment environment)
{
    public const string DefaultDirectory = "App_Data/SalesOrders";
    public const long MaxBytes = 20 * 1024 * 1024;

    public string ResolveDirectory(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory)) throw new InvalidOperationException("請輸入 PDF 儲存路徑。");
        try
        {
            var path = Path.GetFullPath(directory.Trim(), environment.ContentRootPath);
            var webRoot = Path.GetFullPath(environment.WebRootPath).TrimEnd(Path.DirectorySeparatorChar);
            if (path.Equals(webRoot, StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith(webRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("PDF 儲存目錄請設在 wwwroot 以外，讓附件透過登入權限下載。");
            return path;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new InvalidOperationException("PDF 儲存路徑格式不正確。", ex);
        }
    }

    public async Task ValidateDirectoryAsync(string directory)
    {
        var path = ResolveDirectory(directory);
        Directory.CreateDirectory(path);
        var probe = Path.Combine(path, $".write-check-{Guid.NewGuid():N}");
        await using var stream = new FileStream(probe, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose);
        await stream.WriteAsync(new byte[] { 0 });
    }

    public async Task<(string FileName, string StoragePath)> SaveAsync(IFormFile file)
    {
        var fileName = Path.GetFileName(file.FileName.Replace('\\', '/'));
        if (!fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) || fileName.Length > 255)
            throw new InvalidOperationException("請選擇 PDF 檔案，檔名不得超過 255 字元。");
        if (file.Length is <= 0 or > MaxBytes) throw new InvalidOperationException("PDF 檔案必須大於 0 且不可超過 20 MB。");
        await using var source = file.OpenReadStream();
        var signature = new byte[5];
        var count = await source.ReadAtLeastAsync(signature, 5, throwOnEndOfStream: false);
        if (count != 5 || !signature.AsSpan().SequenceEqual("%PDF-"u8))
            throw new InvalidOperationException("檔案內容不是有效的 PDF。");
        var values = await settings.GetSettingsAsync();
        var directory = ResolveDirectory(values.GetValueOrDefault("SALES_ORDER_PDF_DIRECTORY", DefaultDirectory));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"{Guid.NewGuid():N}.pdf");
        try
        {
            await using var target = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await target.WriteAsync(signature);
            await source.CopyToAsync(target);
            return (fileName, path);
        }
        catch
        {
            TryDelete(path);
            throw;
        }
    }

    public void TryDelete(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        try { File.Delete(path); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
