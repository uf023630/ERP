using Microsoft.AspNetCore.Authentication.Cookies;
using SmartIMS.Web.Infrastructure;
using SmartIMS.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<SqlConnectionFactory>();
builder.Services.AddSingleton<DatabaseBootstrapper>();
builder.Services.AddScoped<SystemSettingsService>();
builder.Services.AddScoped<PermissionService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<ListViewSettingsService>();
builder.Services.AddScoped<ProductMaterialService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<QuotationService>();
builder.Services.AddScoped<SalesOrderService>();
builder.Services.AddScoped<OrderPdfStorage>();
builder.Services.AddScoped<CustomerService>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "SmartIMS.Auth";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews(options =>
{
    var messages = options.ModelBindingMessageProvider;
    messages.SetAttemptedValueIsInvalidAccessor((value, field) => $"「{field}」的輸入格式不正確。");
    messages.SetNonPropertyAttemptedValueIsInvalidAccessor(value => "輸入格式不正確。");
    messages.SetUnknownValueIsInvalidAccessor(field => $"「{field}」的輸入值無效。");
    messages.SetNonPropertyUnknownValueIsInvalidAccessor(() => "輸入值無效。");
    messages.SetValueMustNotBeNullAccessor(value => "此欄位不可空白。");
    messages.SetValueIsInvalidAccessor(value => "輸入值無效。");
    messages.SetValueMustBeANumberAccessor(field => $"「{field}」必須是數值。");
    messages.SetNonPropertyValueMustBeANumberAccessor(() => "請輸入有效的數值。");
    messages.SetMissingBindRequiredValueAccessor(field => $"請輸入「{field}」。");
    messages.SetMissingKeyOrValueAccessor(() => "請提供完整的欄位名稱與內容。");
    messages.SetMissingRequestBodyRequiredValueAccessor(() => "請提供要儲存的資料。");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

Directory.CreateDirectory(Path.Combine(app.Environment.WebRootPath, "uploads", "branding"));

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<DatabaseBootstrapper>().EnsureAsync();
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
