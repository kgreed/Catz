using Catz.Module.BusinessObjects;
using DevExpress.EntityFrameworkCore.Security;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EFCore;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.Win;
using DevExpress.ExpressApp.Win.Utils;
using DevExpress.Persistent.BaseImpl.EFCore.AuditTrail;
using Microsoft.EntityFrameworkCore;

namespace Catz.Win;
// For more typical usage scenarios, be sure to check out https://docs.devexpress.com/eXpressAppFramework/DevExpress.ExpressApp.Win.WinApplication._members

public class CatzWindowsFormsApplication : WinApplication
{
    public CatzWindowsFormsApplication()
    {
        SplashScreen = new DXSplashScreen(typeof(XafSplashScreen), new DefaultOverlayFormOptions());
        ApplicationName = "Catz";
        CheckCompatibilityType = DevExpress.ExpressApp.CheckCompatibilityType.DatabaseSchema;
        UseOldTemplates = false;
        DatabaseVersionMismatch += CatzWindowsFormsApplication_DatabaseVersionMismatch;
        CustomizeLanguagesList += CatzWindowsFormsApplication_CustomizeLanguagesList;
    }

    protected override void CreateDefaultObjectSpaceProvider(CreateCustomObjectSpaceProviderEventArgs args)
    {
        var connectionString = args.ConnectionString;
        var handler = new EFCoreDatabaseProviderHandler<CatzEFCoreDbContext>(MakeBuilder, connectionString); // fails here with error cs0149 Method name expected.
        var efCoreObjectSpaceProvider = new SecuredEFCoreObjectSpaceProvider<CatzEFCoreDbContext>(
            (ISelectDataSecurityProvider)Security,
            handler);
        args.ObjectSpaceProviders.Add(efCoreObjectSpaceProvider);
        args.ObjectSpaceProviders.Add(new NonPersistentObjectSpaceProvider(TypesInfo, null));
    }

    private DbContextOptionsBuilder<CatzEFCoreDbContext> MakeBuilder(string connectionString)
    {
        var builder = new DbContextOptionsBuilder<CatzEFCoreDbContext>();
        builder.UseSqlServer(connectionString);
        builder.UseSecurity((ISelectDataSecurityProvider)Security);
        builder.UseAudit();
        return builder;
    }

    private void CatzWindowsFormsApplication_CustomizeLanguagesList(object sender, CustomizeLanguagesListEventArgs e)
    {
        string userLanguageName = System.Threading.Thread.CurrentThread.CurrentUICulture.Name;
        if(userLanguageName != "en-US" && e.Languages.IndexOf(userLanguageName) == -1)
        {
            e.Languages.Add(userLanguageName);
        }
    }

    private void CatzWindowsFormsApplication_DatabaseVersionMismatch(
        object sender,
        DevExpress.ExpressApp.DatabaseVersionMismatchEventArgs e)
    {
#if EASYTEST
        e.Updater.Update();
        e.Handled = true;
#else
        if(System.Diagnostics.Debugger.IsAttached)
        {
            e.Updater.Update();
            e.Handled = true;
        } else
        {
            string message = "The application cannot connect to the specified database, " +
                "because the database doesn't exist, its version is older " +
                "than that of the application or its schema does not match " +
                "the ORM data model structure. To avoid this error, use one " +
                "of the solutions from the https://www.devexpress.com/kb=T367835 KB Article.";
            if(e.CompatibilityError != null && e.CompatibilityError.Exception != null)
            {
                message += "\r\n\r\nInner exception: " + e.CompatibilityError.Exception.Message;
            }
            throw new InvalidOperationException(message);
        }
#endif
    }
}
