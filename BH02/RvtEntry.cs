using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace BH02
{
    public class RvtEntry : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            application.ControlledApplication.DocumentOpened += new EventHandler<DocumentOpenedEventArgs>(Application_DocumentOpened);
            RibbonPanel rpanel = application.CreateRibbonPanel("B1902 HELPER");

            string thisAssemblyPath = Assembly.GetExecutingAssembly().Location;
            PushButtonData bndataAppconfig = new PushButtonData("cmdConfig", "全局设置", thisAssemblyPath, "BH02.Config_Command");
            PushButtonData bndataFamilyNameManager = new PushButtonData("cmdFamilyNameManager", "族名管理", thisAssemblyPath, "BH02.ICommand_Document_FamilyNameManager");

            bndataAppconfig.LargeImage = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(Properties.Resources.sv32.GetHbitmap(),IntPtr.Zero,Int32Rect.Empty,BitmapSizeOptions.FromEmptyOptions());
            bndataFamilyNameManager.LargeImage = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(Properties.Resources.config32.GetHbitmap(),IntPtr.Zero,Int32Rect.Empty,BitmapSizeOptions.FromEmptyOptions());

            rpanel.AddItem(bndataAppconfig);
            rpanel.AddSeparator();
            rpanel.AddItem(bndataFamilyNameManager);

            return Result.Succeeded;
        }

        private void Application_DocumentOpened(object sender, DocumentOpenedEventArgs e)
        {
            /** 數據庫模塊入口
            Document doc = e.Document;
            Global.DocContent = new DocumentContent();
            Global.DataFile = Path.Combine(Path.GetDirectoryName(doc.PathName), $"{Path.GetFileNameWithoutExtension(doc.PathName)}.data");
            Global.SQLDataFile = Path.Combine(Path.GetDirectoryName(doc.PathName), $"{Path.GetFileNameWithoutExtension(doc.PathName)}.db");
            Global.DocContent.CurrentDBContext = new SQLContext($"Data Source={Global.SQLDataFile}");
            Global.DocContent.CurrentDBContext.Database.Create();
            Global.DocContent.CurrentDBContext.SaveChanges();
            **/
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            application.ControlledApplication.DocumentOpened -= new EventHandler<DocumentOpenedEventArgs>(Application_DocumentOpened);
            return Result.Succeeded;
        }
    }

    [Transaction(TransactionMode.Manual)]
    public class ICommand_Document_FamilyNameManager : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            commandData.Application.Application.SharedParametersFilename = Global.GetAppConfig("SharedParametersFile");

            try
            {
                FamilyNameMan ucpe = new FamilyNameMan(commandData);
                Window winaddin = new Window();
                ucpe.ParentWin = winaddin;
                winaddin.Content = ucpe;
                winaddin.SizeToContent = SizeToContent.WidthAndHeight;
                winaddin.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                //winaddin.WindowStyle = WindowStyle.None;
                winaddin.Padding = new Thickness(0);
                Global.winhelper = new System.Windows.Interop.WindowInteropHelper(winaddin);
                winaddin.ShowDialog();
            }
            catch (Exception e)
            {
                TaskDialog.Show("Error", e.ToString());
                return Result.Failed;
            }

            return Result.Succeeded;
        }
    }

}
