using AqlaSerializer;
using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using System.Xml.Serialization;

namespace BH02
{
    public static class Constants
    {
        public const double Pi = 3.14159;
        public const double RVTPrecision = 0.001;
    }
    [SerializableType]
    public class DocumentContent// : INotifyPropertyChanged
    {
        public List<CurtainPanelInfo> CurtainPanelList { get; set; } = new List<CurtainPanelInfo>();
        public Dictionary<int, double> LevelDictionary { get; set; } = new Dictionary<int, double>();
        public List<LevelInfo> LevelList { get; set; } = new List<LevelInfo>();
        public List<ScheduleElementInfo> ScheduleElementList { get; set; } = new List<ScheduleElementInfo>();
        public List<DeepElementInfo> DeepElementList { get; set; } = new List<DeepElementInfo>();
        public ObservableCollection<ZoneInfoBase> ZoneList { get; set; } = new ObservableCollection<ZoneInfoBase>();
        public List<MullionInfo> MullionList { get; set; } = new List<MullionInfo>();

        public List<ExternalElementData> ExternalElementDataList { get; set; } = new List<ExternalElementData>();
        public List<CurtainPanelInfo> ExternalCurtainPanelList { get; set; } = new List<CurtainPanelInfo>();
        public List<ScheduleElementInfo> ExternalScheduleElementList { get; set; } = new List<ScheduleElementInfo>();
        public List<CurtainPanelInfo> FullCurtainPanelList { get; set; } = new List<CurtainPanelInfo>();
        public List<ScheduleElementInfo> FullScheduleElementList { get; set; } = new List<ScheduleElementInfo>();
        public ObservableCollection<ZoneInfoBase> FullZoneList { get; set; } = new ObservableCollection<ZoneInfoBase>();

        [NonSerializableMember] public List<ParameterHelper.RawProjectParameterInfo> ParameterInfoList { get; set; } = new List<ParameterHelper.RawProjectParameterInfo>();

        public ZoneInfoBase CurrentZoneInfo = new ZoneInfoBase();

        //public SQLContext CurrentDBContext;
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public DocumentContent()
        {
        }
    }

    [SerializableType]
    public class ExternalElementData
    {
        public int ExternalId { get; set; }
        public string ExternalFileName { get; set; }
        public List<CurtainPanelInfo> CurtainPanelList { get; set; } = new List<CurtainPanelInfo>();
        public List<ScheduleElementInfo> ScheduleElementList { get; set; } = new List<ScheduleElementInfo>();
        public List<ZoneInfoBase> ZoneList { get; set; } = new List<ZoneInfoBase>();
    }

    [Serializable, XmlRoot(Namespace = "", IsNullable = false)]
    [SerializableType]
    public class ZoneLayerInfo
    {
        [XmlAttribute] public string HandleId { get; set; }
        [XmlAttribute] public string ZoneCode { get; set; }
        [XmlAttribute] public int ZoneLayer { get; set; }
        [XmlAttribute] public DateTime ZoneStart { get; set; }
        [XmlAttribute] public DateTime ZoneFinish { get; set; }
        [XmlAttribute] public int ZoneDays { get; set; }
        [XmlAttribute] public int ZoneHours { get; set; }

        public ZoneLayerInfo() { }
    }

    [SerializableType]
    public class ZoneScheduleLayerInfo
    {
        public string HandleId { get; set; }
        public string ZoneUniversalCode { get; set; }
        public DateTime[] ZoneLayerStart { get; set; } = new DateTime[3];
        public DateTime[] ZoneLayerFinish { get; set; } = new DateTime[3];
    }

    [Serializable, XmlRoot(Namespace = "", IsNullable = false)]
    [SerializableType]
    public class ElementClass : INotifyPropertyChanged
    {
        private int _eClassIndex;
        private string _eClassName = "Unset";
        private bool _isScheduled = false;
        private int _eTaskLayer = -1;
        private int _eTaskSubLayer = -1;

        [XmlAttribute] public int EClassIndex { get { return _eClassIndex; } set { _eClassIndex = value; OnPropertyChanged(nameof(EClassIndex)); } }
        [XmlAttribute] public string EClassName { get { return _eClassName; } set { _eClassName = value; OnPropertyChanged(nameof(EClassName)); } }
        [XmlAttribute] public bool IsScheduled { get { return _isScheduled; } set { _isScheduled = value; OnPropertyChanged(nameof(IsScheduled)); } }
        [XmlAttribute] public int ETaskLayer { get { return _eTaskLayer; } set { _eTaskLayer = value; OnPropertyChanged(nameof(ETaskLayer)); } }
        [XmlAttribute] public int ETaskSubLayer { get { return _eTaskSubLayer; } set { _eTaskSubLayer = value; OnPropertyChanged(nameof(ETaskSubLayer)); } }

        public ElementClass()
        {
            EClassName = "Unset";
            IsScheduled = false;
            ETaskLayer = -1;
            ETaskSubLayer = -1;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    [Serializable, XmlRoot(Namespace = "", IsNullable = false)]
    [SerializableType]
    public class ElementIndexRange : INotifyPropertyChanged
    {
        private string _zoneCode = "";
        private int _elementType = 0;
        private int _indexMax = 0;

        [XmlAttribute] public string ZoneCode { get { return _zoneCode; } set { _zoneCode = value; OnPropertyChanged(nameof(ZoneCode)); } }
        [XmlAttribute] public int ElementType { get { return _elementType; } set { _elementType = value; OnPropertyChanged(nameof(ElementType)); } }
        [XmlAttribute] public int IndexMax { get { return _indexMax; } set { _indexMax = value; OnPropertyChanged(nameof(IndexMax)); } }

        public ElementIndexRange() { ZoneCode = "Unset"; ElementType = 0; IndexMax = 0; }
        public ElementIndexRange(string zonecode) : this() { ZoneCode = zonecode; }
        public ElementIndexRange(string zonecode, int etype) : this() { ZoneCode = zonecode; ElementType = etype; }
        public ElementIndexRange(string zonecode, int etype, int max) : this() { ZoneCode = zonecode; ElementType = etype; IndexMax = max; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    [Serializable, XmlRoot(Namespace = "", IsNullable = false)]
    [SerializableType]
    public class ElementFabricationInfo : INotifyPropertyChanged
    {
        private int _elementType = 0;
        private string _elementCode = string.Empty;
        private string _fabrCode = string.Empty;
        private int _fabrQuantity = 0;
        private string _orderCode = string.Empty;

        [XmlAttribute] public int ElementType { get { return _elementType; } set { _elementType = value; OnPropertyChanged(nameof(ElementType)); } }
        [XmlAttribute] public string ElementCode { get { return _elementCode; } set { _elementCode = value; OnPropertyChanged(nameof(ElementCode)); } }
        [XmlAttribute] public string FabrCode { get { return _fabrCode; } set { _fabrCode = value; OnPropertyChanged(nameof(FabrCode)); } }
        [XmlAttribute] public int FabrQuantity { get { return _fabrQuantity; } set { _fabrQuantity = value; OnPropertyChanged(nameof(FabrQuantity)); } }
        [XmlAttribute] public string OrderCode { get { return _orderCode; } set { _orderCode = value; OnPropertyChanged(nameof(OrderCode)); } }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class Global
    {
        public static DocumentContent DocContent;
        public static string DataFile;
        public static WindowInteropHelper winhelper;

        public static int OptionHoursPerDay = 8;
        public static int OptionDaysPerWeek = 7;

        public static List<ElementIndexRange> ElementIndexRangeList = new List<ElementIndexRange>();
        public static List<ElementClass> ElementClassList = new List<ElementClass>();
        public static List<ZoneLayerInfo> ZoneLayerList = new List<ZoneLayerInfo>();
        public static int[][] TaskLevelClass;

        public static void UpdateAppConfig(string newKey, string newValue)
        {
            bool isModified = false;
            Configuration config = ConfigurationManager.OpenExeConfiguration(System.Reflection.Assembly.GetExecutingAssembly().Location);

            foreach (string key in config.AppSettings.Settings.AllKeys)
            {
                if (key == newKey)
                {
                    isModified = true;
                }
            }

            if (isModified) config.AppSettings.Settings.Remove(newKey);
            config.AppSettings.Settings.Add(newKey, newValue);
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        public static string GetAppConfig(string strKey)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(System.Reflection.Assembly.GetExecutingAssembly().Location);
            foreach (string key in config.AppSettings.Settings.AllKeys)
            {
                if (key == strKey)
                {
                    return config.AppSettings.Settings[strKey].Value;
                }
            }
            return null;
        }

        public static ElementIndexRange GetElementIndexRange(string zonecode, int etype)
        {
            ElementIndexRange eir = new ElementIndexRange(zonecode, etype);
            if (ElementIndexRangeList?.Count == 0)
            {
                ElementIndexRangeList.Add(eir);
                return eir;
            }
            else
            {
                return ElementIndexRangeList.FirstOrDefault(r => r.ZoneCode.Equals(zonecode, StringComparison.CurrentCultureIgnoreCase) && r.ElementType == etype) ?? eir;
            }
        }

        public static void UpdateElementIndexRange(string zonecode, int etype, int max)
        {
            ElementIndexRangeList.RemoveAll(r => r.ZoneCode.Equals(zonecode, StringComparison.CurrentCultureIgnoreCase) && r.ElementType == etype);
            ElementIndexRangeList.Add(new ElementIndexRange(zonecode, etype, max));
        }

    }
    
    public class IntToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)value == int.Parse(parameter.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var data = (bool)value;
            if (data)
            {
                return System.Convert.ToInt32(parameter);
            }
            return -1;
        }
    }

    public class VarToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.Equals(parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.Equals(true) ? parameter : System.Windows.Data.Binding.DoNothing;
        }
    }


    public class PrefixConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return false;
            return (value as string).StartsWith(parameter as string);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.Equals(true) ? parameter : System.Windows.Data.Binding.DoNothing;
        }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool bValue = (bool)value;
            if (bValue) return System.Windows.Visibility.Visible;
            else return System.Windows.Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            System.Windows.Visibility visibility = (System.Windows.Visibility)value;

            if (visibility == System.Windows.Visibility.Visible) return true;
            else return false;
        }
    }

    public class ParamName2ValueMultiValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            int eleid = (int)values[0]; //INF_ElementId
            string pname = (string)values[1]; //ParamName
            Document doc = (Document) values[2];
            if (pname is null)
                return string.Empty;
            else
                return  doc.GetElement(new ElementId(eleid)).LookupParameter(pname).AsValueString();
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public class NegateBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !(bool)value;
        }
    }

    public class MultiValueElementClassConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool bsch = (bool)values[0]; //IsScheduled, bool
            int ilayer = (int)values[1]; //ETaskLayer, int
            int ilc = int.Parse(parameter as string);
            if (bsch && ilayer == ilc)
            {
                return System.Windows.Visibility.Visible;
            }
            return System.Windows.Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class NotNullValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            if (string.IsNullOrEmpty(value as string) || string.IsNullOrWhiteSpace(value as string))
            {
                return new ValidationResult(false, "不能为空！");
            }
            return new ValidationResult(true, null);
        }
    }
    public class ProjectIDRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string _id = value as string;

            if (!string.IsNullOrWhiteSpace(_id))
            {
                string pIDFormartRegex = @"^BIM\d{10}[C|W]$";

                // 检查输入的字符串是否符合IP地址格式
                if (!System.Text.RegularExpressions.Regex.IsMatch(_id, pIDFormartRegex))
                {
                    return new ValidationResult(false, "项目编号格式不正确");
                }
            }
            return new ValidationResult(true, null);
        }
    }
}
