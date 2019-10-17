using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BH02
{
    /// <summary>
    /// Interaction logic for FamilyNameMan.xaml
    /// </summary>
    public partial class FamilyNameMan : UserControl, INotifyPropertyChanged
    {
        public Window ParentWin { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private int _currentOperType = 0; // 1:<, 2:>, 0:=.
        public int CurrentOperType { get { return _currentOperType; } set { _currentOperType = value; OnPropertyChanged(nameof(_currentOperType)); } }


        private UIApplication _uiapp;
        private UIDocument _uidoc;
        private Document _doc;
        private ExternalCommandData _cdata;
        private ICollection<ElementId> _sids;
        public Document Doc { get => _doc; set { _doc = value; OnPropertyChanged(nameof(Doc)); } }

        private ObservableCollection<string> _familyNameListSource = new ObservableCollection<string>();
        private List<Element> _currentElementList = new List<Element>();
        private ObservableCollection<ScheduleElementInfo> _currentElementInfoList = new ObservableCollection<ScheduleElementInfo>();
        private ObservableCollection<ScheduleElementInfo> _appliedElementInfoList = new ObservableCollection<ScheduleElementInfo>();
        private ObservableCollection<FamilyNameFilterInfo> _currentFamilyNameFilterInfoList = new ObservableCollection<FamilyNameFilterInfo>();
        public List<Element> CurrentElementList { get { return _currentElementList; } set { _currentElementList = value; OnPropertyChanged(nameof(CurrentElementList)); } }
        public ObservableCollection<ScheduleElementInfo> CurrentElementInfoList { get { return _currentElementInfoList; } set { _currentElementInfoList = value; OnPropertyChanged(nameof(CurrentElementInfoList)); } }
        public ObservableCollection<ScheduleElementInfo> AppliedElementInfoList { get { return _appliedElementInfoList; } set { _appliedElementInfoList = value; OnPropertyChanged(nameof(AppliedElementInfoList)); } }
        public ObservableCollection<string> FamilyNameListSource { get { return _familyNameListSource; } set { _familyNameListSource = value; OnPropertyChanged(nameof(FamilyNameListSource)); } }
        public ObservableCollection<FamilyNameFilterInfo> CurrentFamilyNameFilterInfoList  { get { return _currentFamilyNameFilterInfoList; } set { _currentFamilyNameFilterInfoList = value; OnPropertyChanged(nameof(CurrentFamilyNameFilterInfoList)); } }




        public FamilyNameMan(ExternalCommandData commandData)
        {
            InitializeComponent();
            this.DataContext = this;

            _cdata = commandData;
            _uiapp = commandData.Application;
            _uidoc = _uiapp.ActiveUIDocument;
            _doc = _uidoc.Document;
            _sids = _uidoc.Selection.GetElementIds();

            //var pwlist = new FilteredElementCollector(_doc).WherePasses(new LogicalAndFilter(new ElementClassFilter(typeof(FamilyInstance)), new ElementCategoryFilter(BuiltInCategory.OST_CurtainWallPanels)));
            //.Union(new FilteredElementCollector(_doc).WherePasses(new LogicalAndFilter(new ElementClassFilter(typeof(FamilyInstance)), new ElementCategoryFilter(BuiltInCategory.OST_Windows))));
            var finstlist = new FilteredElementCollector(_doc).OfClass(typeof(Family)).ToElements();

            FamilyNameListSource.Clear();
            foreach (Element e in finstlist)
            {
                string fname = (e as Family).Name;
                FamilyNameListSource.Add(fname);
                CurrentFamilyNameFilterInfoList.Add(new FamilyNameFilterInfo() { INF_Oper=CurrentOperType, INF_OperValue= cbFamilyNameValueList.Text});//
            }

            var mru = Global.GetAppConfig("FamilyNameValueMRU")?.Split('|');
            if (mru != null) foreach (string v in mru) cbFamilyNameValueList.Items.Add(v);

            InitializeCommand();


        }

        private void InitializeCommand()
        {
            ;
        }

        public FamilyNameMan()
        {
            InitializeComponent();
        }

        private void listInformation_SelectionChanged(object sender, SelectionChangedEventArgs e) { var lb = sender as ListBox; lb.ScrollIntoView(lb.Items[lb.Items.Count - 1]); }
    }

    public class FamilyNameFilterInfo : INotifyPropertyChanged
    {
        private int _inf_Oper;
        private string _inf_OperValue;
        public int INF_Oper { get { return _inf_Oper; } set { _inf_Oper = value; OnPropertyChanged(nameof(INF_Oper)); } }
        public string INF_OperValue { get { return _inf_OperValue; } set { _inf_OperValue = value; OnPropertyChanged(nameof(INF_OperValue)); } }

        string[] __stropers = { " = ", " < ", " > " };
        public override string ToString() => $"族名{__stropers[INF_Oper]}{INF_OperValue}";

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    }
}
