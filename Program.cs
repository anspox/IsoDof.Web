using Microsoft.EntityFrameworkCore;
using IsoDof.Web.Data;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews()
    .AddMvcOptions(options =>
    {
        var p = options.ModelBindingMessageProvider;
        p.SetValueIsInvalidAccessor(v => $"'{v}' değeri geçersiz.");
        p.SetValueMustNotBeNullAccessor(v => "Bu alan zorunludur.");
        p.SetAttemptedValueIsInvalidAccessor((v, f) => $"'{v}' değeri '{f}' alanı için geçersiz.");
        p.SetMissingBindRequiredValueAccessor(f => $"'{f}' alanı zorunludur.");
        p.SetMissingKeyOrValueAccessor(() => "Bu alan zorunludur.");
        p.SetNonPropertyValueMustBeANumberAccessor(() => "Bir sayı giriniz.");
        p.SetValueMustBeANumberAccessor(f => $"'{f}' alanına bir sayı giriniz.");
        p.SetUnknownValueIsInvalidAccessor(f => $"'{f}' alanına girilen değer geçersiz.");
    });

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
