using DinkToPdf;
using DinkToPdf.Contracts;
using Sheessential_Sales_Finance.helpers;
using System.Reflection;
using System.Runtime.Loader;

//  Load native DinkToPdf library


var builder = WebApplication.CreateBuilder(args);

// Load libwkhtmltox.dll (important!)
var context = new CustomAssemblyLoadContext();
context.LoadUnmanagedLibrary(
    Path.Combine(Directory.GetCurrentDirectory(), "DinkToPdf", "lib", "64bit", "libwkhtmltox.dll")
);

// Add services to the container
builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register DinkToPdf converter
builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));

// Register MongoHelper for database access
builder.Services.AddSingleton<MongoHelper>();

var app = builder.Build();

// Middleware setup
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseAuthorization();
app.UseExceptionHandler("/Error/Connection");

//  Session guard middleware
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLower();

    // Allow public routes
    if (path != null &&
        (path.Contains("/auth/login") ||
        path.Contains("/auth/forgotpassword") ||
        path.Contains("/auth/resetpassword") ||
         path.Contains("/auth/register") ||
         path.Contains("/sales_finance/gethumanresourcetables") ||
         path.Contains("/sales_finance/gethumanresourcetableattributes") ||
         path.Contains("/sales_finance/removeduplicateexpensesbyexpenseid") ||
         path.Contains("/css") ||
         path.Contains("/js") ||
         path.Contains("/images") ||
         path.Contains("/error/connection")))
    {
        await next();
    }
    else
    {
        // Redirect if session is missing
        if (string.IsNullOrEmpty(context.Session.GetString("UserId")))
        {
            context.Response.Redirect("/Auth/Login");
        }
        else
        {
            if (path != null &&
                path.Contains("/sales_finance/executivepayrollapproval"))
            {
                var userRole = (context.Session.GetString("UserRole") ?? string.Empty).Trim();
                var userDepartment = (context.Session.GetString("UserDepartment") ?? string.Empty).Trim();
                var isFinanceManager =
                    userRole.Equals("Finance manager", StringComparison.OrdinalIgnoreCase) ||
                    (userDepartment.Equals("Finance", StringComparison.OrdinalIgnoreCase) &&
                     userRole.Contains("manager", StringComparison.OrdinalIgnoreCase));

                if (!isFinanceManager)
                {
                    context.Response.Redirect("/Sales_Finance/Dashboard");
                    return;
                }
            }

            await next();
        }
    }
});
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}"
);

app.Run();
