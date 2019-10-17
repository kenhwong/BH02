using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using RvtApplication = Autodesk.Revit.ApplicationServices.Application;


namespace BH02
{
    /// <summary>
    /// Provides static functions to convert unit
    /// </summary>
    static class Unit
    {
        #region Unit API Methods
        /// <summary>
        /// Convert the value get from RevitAPI to the value indicated by DisplayUnitType
        /// </summary>
        /// <param name="to">DisplayUnitType indicates unit of target value</param>
        /// <param name="value">value get from RevitAPI</param>
        /// <returns>Target value</returns>
        public static double CovertFromAPI(DisplayUnitType to, double value)
        {
            return value *= ImperialDutRatio(to);
        }

        /// <summary>
        /// Convert a value indicated by DisplayUnitType to the value used by RevitAPI
        /// </summary>
        /// <param name="value">Value to be converted</param>
        /// <param name="from">DisplayUnitType indicates the unit of the value to be converted</param>
        /// <returns>Target value</returns>
        public static double CovertToAPI(double value, DisplayUnitType from)
        {
            return value /= ImperialDutRatio(from);
        }

        /// <summary>
        /// Get ratio between value in RevitAPI and value to display indicated by DisplayUnitType
        /// </summary>
        /// <param name="dut">DisplayUnitType indicates display unit type</param>
        /// <returns>Ratio </returns>
        private static double ImperialDutRatio(DisplayUnitType dut)
        {
            switch (dut)
            {
                case DisplayUnitType.DUT_DECIMAL_FEET: return 1;
                case DisplayUnitType.DUT_FEET_FRACTIONAL_INCHES: return 1;
                case DisplayUnitType.DUT_DECIMAL_INCHES: return 12;
                case DisplayUnitType.DUT_FRACTIONAL_INCHES: return 12;
                case DisplayUnitType.DUT_METERS: return 0.3048;
                case DisplayUnitType.DUT_CENTIMETERS: return 30.48;
                case DisplayUnitType.DUT_MILLIMETERS: return 304.8;
                case DisplayUnitType.DUT_METERS_CENTIMETERS: return 0.3048;
                default: return 1;
            }
        }
        #endregion
    }

    public class ProjectParameterData
    {
        public Definition Definition = null;
        public ElementBinding Binding = null;
        public bool IsSharedStatusKnown = false;  // Will probably always be true when the data is gathered
        public bool IsShared = false;
        public string GUID = null;
    }

    #region ParameterHelper

    public static class ParameterHelper
    {

        #region RawProjectParameterInfo

        public class RawProjectParameterInfo
        {
            public static string FileName { get; set; }
            public string Name { get; set; }
            public BuiltInParameterGroup Group { get; set; }
            public ParameterType Type { get; set; }
            public bool ReadOnly { get; set; }
            public bool BoundToInstance { get; set; }
            public string[] BoundCategories { get; set; }
            public bool FromShared { get; set; }
            public string GUID { get; set; }
            public string Owner { get; set; }
            public bool Visible { get; set; }
        }

        public static List<T> RawConvertSetToList<T>(IEnumerable set)
        {
            List<T> list = (from T p in set select p).ToList<T>();
            return list;
        }

        public static List<RawProjectParameterInfo> RawGetProjectParametersInfo(Document doc)
        {
            RawProjectParameterInfo.FileName = doc.Title;
            List<RawProjectParameterInfo> paramList = new List<RawProjectParameterInfo>();

            BindingMap map = doc.ParameterBindings;
            DefinitionBindingMapIterator it = map.ForwardIterator();
            it.Reset();
            while (it.MoveNext())
            {
                ElementBinding eleBinding = it.Current as ElementBinding;
                InstanceBinding insBinding = eleBinding as InstanceBinding;
                Definition def = it.Key;
                if (def != null)
                {
                    ExternalDefinition extDef = def as ExternalDefinition;
                    bool shared = extDef != null;
                    RawProjectParameterInfo param = new RawProjectParameterInfo
                    {
                        Name = def.Name,
                        Group = def.ParameterGroup,
                        Type = def.ParameterType,
                        ReadOnly = true, // def.IsReadOnly, def.IsReadOnly NOT working in either 2015 or 2014 but working in 2013
                        BoundToInstance = insBinding != null,
                        BoundCategories = RawConvertSetToList<Category>(eleBinding.Categories).Select(c => c.Name).ToArray(),

                        FromShared = shared,
                        GUID = shared ? extDef.GUID.ToString() : string.Empty,
                        Owner = shared ? extDef.OwnerGroup.Name : string.Empty,
                        Visible = shared ? extDef.Visible : true,
                    };

                    paramList.Add(param);
                }
            }

            return paramList;
        }

        /// <summary>
        /// convert the informative object List into a single string
        /// </summary>
        /// <param name="infoList"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        public static string RawParametersInfoToCSVString(List<RawProjectParameterInfo> infoList, ref string title)
        {
            StringBuilder sb = new StringBuilder();
            PropertyInfo[] propInfoArray = typeof(RawProjectParameterInfo).GetProperties();
            foreach (PropertyInfo pi in propInfoArray)
            {
                title += pi.Name + ",";
            }
            title = title.Remove(title.Length - 1);

            foreach (RawProjectParameterInfo info in infoList)
            {
                foreach (PropertyInfo pi in propInfoArray)
                {
                    object obj = info.GetType().InvokeMember(pi.Name, BindingFlags.GetProperty, null, info, null);
                    IList list = obj as IList;
                    if (list != null)
                    {
                        string str = string.Empty;
                        foreach (object e in list)
                        {
                            str += e.ToString() + ";";
                        }
                        str = str.Remove(str.Length - 1);

                        sb.Append(str + ",");
                    }
                    else
                    {
                        sb.Append((obj == null ? string.Empty : obj.ToString()) + ",");
                    }
                }
                sb.Remove(sb.Length - 1, 1).Append(Environment.NewLine);
            }

            return sb.ToString();
        }


        #endregion


        #region Create Project Parameter

        public static void RawCreateProjectParameterFromExistingSharedParameter(RvtApplication app, string name, CategorySet cats, BuiltInParameterGroup group, bool inst)
        {
            DefinitionFile defFile = app.OpenSharedParameterFile();
            if (defFile == null) throw new Exception("No SharedParameter File!");

            var v = (from DefinitionGroup dg in defFile.Groups
                     from ExternalDefinition d in dg.Definitions
                     where d.Name == name
                     select d);
            if (v == null || v.Count() < 1) throw new Exception("Invalid Name Input!");

            ExternalDefinition def = v.First();

            Autodesk.Revit.DB.Binding binding = app.Create.NewTypeBinding(cats);
            if (inst) binding = app.Create.NewInstanceBinding(cats);

            BindingMap map = (new UIApplication(app)).ActiveUIDocument.Document.ParameterBindings;
            map.Insert(def, binding, group);
        }

        public static void RawCreateProjectParameterFromNewSharedParameter(RvtApplication app, string defGroup, string name, ParameterType type, bool visible, CategorySet cats, BuiltInParameterGroup paramGroup, bool inst)
        {
            DefinitionFile defFile = app.OpenSharedParameterFile();
            if (defFile == null) throw new Exception("No SharedParameter File!");

            //ExternalDefinition def = app.OpenSharedParameterFile().Groups.Create(defGroup).Definitions.Create(name, type, visible) as ExternalDefinition;
            ExternalDefinition def = app.OpenSharedParameterFile().Groups.Create(defGroup).Definitions.Create(new ExternalDefinitionCreationOptions(name, type)) as ExternalDefinition;

            Autodesk.Revit.DB.Binding binding = app.Create.NewTypeBinding(cats);
            if (inst) binding = app.Create.NewInstanceBinding(cats);

            BindingMap map = (new UIApplication(app)).ActiveUIDocument.Document.ParameterBindings;
            map.Insert(def, binding, paramGroup);
        }

        public static void RawCreateProjectParameter(RvtApplication app, string name, ParameterType type, bool visible, CategorySet cats, BuiltInParameterGroup group, bool inst)
        {
            //InternalDefinition def = new InternalDefinition();
            //Definition def = new Definition();

            string oriFile = app.SharedParametersFilename;
            string tempFile = System.IO.Path.GetTempFileName() + ".txt";
            using (System.IO.File.Create(tempFile)) { }
            app.SharedParametersFilename = tempFile;

            ExternalDefinition def = app.OpenSharedParameterFile().Groups.Create("TemporaryDefintionGroup").Definitions.Create(new ExternalDefinitionCreationOptions(name, type)) as ExternalDefinition;

            app.SharedParametersFilename = oriFile;
            System.IO.File.Delete(tempFile);

            Autodesk.Revit.DB.Binding binding = app.Create.NewTypeBinding(cats);
            if (inst) binding = app.Create.NewInstanceBinding(cats);

            BindingMap map = (new UIApplication(app)).ActiveUIDocument.Document.ParameterBindings;
            map.Insert(def, binding, group);
        }

        #endregion

        #region project parameter guid
        /// <summary>
        /// This class contains information discovered about a (shared or non-shared) project parameter 
        /// </summary>


        // ================= HELPER METHODS ======================================================================================



        /// <summary>
        /// Returns a list of the objects containing references to the project parameter definitions
        /// </summary>
        /// <param name="projectDocument">The project document being quereied</param>
        /// <returns></returns>
        public static List<ProjectParameterData> GetProjectParameterData(Document projectDocument)
        {
            // Following good SOA practices, first validate incoming parameters
            if (projectDocument == null)
            {
                throw new ArgumentNullException("projectDocument");
            }

            if (projectDocument.IsFamilyDocument)
            {
                throw new Exception("projectDocument can not be a family document.");
            }

            List<ProjectParameterData> result = new List<ProjectParameterData>();
            BindingMap map = projectDocument.ParameterBindings;
            DefinitionBindingMapIterator it = map.ForwardIterator();
            it.Reset();
            while (it.MoveNext())
            {
                ProjectParameterData newProjectParameterData = new ProjectParameterData() { Definition = it.Key, Binding = it.Current as ElementBinding };
                result.Add(newProjectParameterData);
            }

            return result;
        }



        /// <summary>
        /// This method takes a category and information about a project parameter and 
        /// adds a binding to the category for the parameter.  It will throw an exception if the parameter
        /// is already bound to the desired category.  It returns whether or not the API reports that it
        /// successfully bound the parameter to the desired category.
        /// </summary>
        /// <param name="projectDocument">The project document in which the project parameter has been defined</param>
        /// <param name="projectParameterData">Information about the project parameter</param>
        /// <param name="category">The additional category to which to bind the project parameter</param>
        /// <returns></returns>
        public static bool AddProjectParameterBinding(Document projectDocument,
                                                       ProjectParameterData projectParameterData,
                                                       Category category)
        {
            // Following good SOA practices, first validate incoming parameters
            if (projectDocument == null)
            {
                throw new ArgumentNullException("projectDocument");
            }

            if (projectDocument.IsFamilyDocument)
            {
                throw new Exception("projectDocument can not be a family document.");
            }

            if (projectParameterData == null)
            {
                throw new ArgumentNullException("projectParameterData");
            }

            if (category == null)
            {
                throw new ArgumentNullException("category");
            }

            bool result = false;
            CategorySet cats = projectParameterData.Binding.Categories;

            if (cats.Contains(category))
            {
                // It's already bound to the desired category.  Nothing to do.
                string errorMessage = string.Format("The project parameter '{0}' is already bound to the '{1}' category.",
                                                    projectParameterData.Definition.Name,
                                                    category.Name);

                throw new Exception(errorMessage);
            }

            cats.Insert(category);

            // See if the parameter is an instance or type parameter.
            InstanceBinding instanceBinding = projectParameterData.Binding as InstanceBinding;

            if (instanceBinding != null)
            {
                // Is an Instance parameter
                InstanceBinding newInstanceBinding = projectDocument.Application.Create.NewInstanceBinding(cats);
                if (projectDocument.ParameterBindings.ReInsert(projectParameterData.Definition, newInstanceBinding))
                {
                    result = true;
                }
            }
            else
            {
                // Is a type parameter
                TypeBinding typeBinding = projectDocument.Application.Create.NewTypeBinding(cats);
                if (projectDocument.ParameterBindings.ReInsert(projectParameterData.Definition, typeBinding))
                {
                    result = true;
                }
            }

            return result;
        }




        /// <summary>
        /// This method populates the appropriate values on a ProjectParameterData object with information from
        /// the given Parameter object.
        /// </summary>
        /// <param name="parameter">The Parameter object with source information</param>
        /// <param name="projectParameterDataToFill">The ProjectParameterData object to fill</param>
        public static void PopulateProjectParameterData(Parameter parameter,
                                                         ProjectParameterData projectParameterDataToFill)
        {
            // Following good SOA practices, validat incoming parameters first.
            if (parameter == null)
            {
                throw new ArgumentNullException("parameter");
            }

            if (projectParameterDataToFill == null)
            {
                throw new ArgumentNullException("projectParameterDataToFill");
            }

            projectParameterDataToFill.IsSharedStatusKnown = true;
            projectParameterDataToFill.IsShared = parameter.IsShared;
            if (parameter.IsShared)
            {
                if (parameter.GUID != null)
                {
                    projectParameterDataToFill.GUID = parameter.GUID.ToString();
                }
            }

        }  // end of PopulateProjectParameterData

        #endregion


        #region 初始化项目参数
        public static void InitProjectParameters(ref Document doc)
        {
            #region 设置项目参数

            using (Transaction trans = new Transaction(doc, "CreateProjectParameters"))
            {
                trans.Start();
                #region 设置项目参数：立面朝向
                if (!Global.DocContent.ParameterInfoList.Exists(x => x.Name == "立面朝向"))
                {
                    CategorySet _catset = new CategorySet();
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_CurtainWallPanels));
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_GenericModel));
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_CurtainWallMullions));
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_Windows));
                    ParameterHelper.RawCreateProjectParameter(doc.Application, "立面朝向", ParameterType.Text, true, _catset, BuiltInParameterGroup.PG_DATA, true); 
                }
                #endregion
                #region 设置项目参数：立面系统
                if (!Global.DocContent.ParameterInfoList.Exists(x => x.Name == "立面系统"))
                {
                    CategorySet _catset = new CategorySet();
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_CurtainWallPanels));
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_GenericModel));
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_CurtainWallMullions));
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_Windows));
                    ParameterHelper.RawCreateProjectParameter(doc.Application, "立面系统", ParameterType.Text, true, _catset, BuiltInParameterGroup.PG_DATA, true);
                }
                #endregion
                #region 设置项目参数：立面楼层
                if (!Global.DocContent.ParameterInfoList.Exists(x => x.Name == "立面楼层"))
                {
                    CategorySet _catset = new CategorySet();
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_CurtainWallPanels));
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_GenericModel));
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_CurtainWallMullions));
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_Windows));
                    ParameterHelper.RawCreateProjectParameter(doc.Application, "立面楼层", ParameterType.Integer, true, _catset, BuiltInParameterGroup.PG_DATA, true);
                }
                #endregion
                #region 设置项目参数：构件分项
                if (!Global.DocContent.ParameterInfoList.Exists(x => x.Name == "构件分项"))
                {
                    CategorySet _catset = new CategorySet();
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_CurtainWallPanels));
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_GenericModel));
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_CurtainWallMullions));
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_Windows));
                    ParameterHelper.RawCreateProjectParameter(doc.Application, "构件分项", ParameterType.Integer, true, _catset, BuiltInParameterGroup.PG_DATA, true);
                }
                #endregion
                #region 设置项目参数：构件子项
                if (!Global.DocContent.ParameterInfoList.Exists(x => x.Name == "构件子项"))
                {
                    CategorySet _catset = new CategorySet();
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_CurtainWallPanels));
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_GenericModel));
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_CurtainWallMullions));
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_Windows));
                    ParameterHelper.RawCreateProjectParameter(doc.Application, "构件子项", ParameterType.Text, true, _catset, BuiltInParameterGroup.PG_DATA, true);
                }
                #endregion
                #region 设置项目参数：加工编号
                if (!Global.DocContent.ParameterInfoList.Exists(x => x.Name == "加工编号"))
                {
                    CategorySet _catset = new CategorySet();
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_GenericModel));
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_CurtainWallMullions));
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_Windows));
                    ParameterHelper.RawCreateProjectParameter(doc.Application, "加工编号", ParameterType.Text, true, _catset, BuiltInParameterGroup.PG_DATA, true);
                }
                #endregion
                #region 设置项目参数：材料单号
                if (!Global.DocContent.ParameterInfoList.Exists(x => x.Name == "材料单号"))
                {
                    CategorySet _catset = new CategorySet();
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_GenericModel));
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_CurtainWallMullions));
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_Windows));
                    ParameterHelper.RawCreateProjectParameter(doc.Application, "材料单号", ParameterType.Text, true, _catset, BuiltInParameterGroup.PG_DATA, true);
                }
                #endregion

                #region 设置项目参数：分区序号
                if (!Global.DocContent.ParameterInfoList.Exists(x => x.Name == "分区序号"))
                {
                    CategorySet _catset = new CategorySet();
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_CurtainWallPanels));
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_GenericModel));
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_CurtainWallMullions));
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_Windows));
                    ParameterHelper.RawCreateProjectParameter(doc.Application, "分区序号", ParameterType.Integer, true, _catset, BuiltInParameterGroup.PG_DATA, true);
                }
                #endregion

                #region 设置项目参数：分区区号
                if (!Global.DocContent.ParameterInfoList.Exists(x => x.Name == "分区区号"))
                {
                    CategorySet _catset = new CategorySet();
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_CurtainWallPanels));
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_GenericModel));
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_CurtainWallMullions));
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_Windows));
                    ParameterHelper.RawCreateProjectParameter(doc.Application, "分区区号", ParameterType.Text, true, _catset, BuiltInParameterGroup.PG_DATA, true);
                }
                #endregion

                #region 设置项目参数：分区编码
                if (!Global.DocContent.ParameterInfoList.Exists(x => x.Name == "分区编码"))
                {
                    CategorySet _catset = new CategorySet();
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_CurtainWallPanels));
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_GenericModel));
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_CurtainWallMullions));
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_Windows));
                    ParameterHelper.RawCreateProjectParameter(doc.Application, "分区编码", ParameterType.Text, true, _catset, BuiltInParameterGroup.PG_DATA, true);
                }
                #endregion

                #region 设置项目参数：進場時間
                if (!Global.DocContent.ParameterInfoList.Exists(x => x.Name == "进场时间"))
                {
                    CategorySet _catset = new CategorySet();
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_CurtainWallPanels));
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_GenericModel));
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_CurtainWallMullions));
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_Windows));
                    ParameterHelper.RawCreateProjectParameter(doc.Application, "进场时间", ParameterType.Text, true, _catset, BuiltInParameterGroup.PG_PHASING, true);
                }
                #endregion

                #region 设置项目参数：安裝開始
                if (!Global.DocContent.ParameterInfoList.Exists(x => x.Name == "安装开始"))
                {
                    CategorySet _catset = new CategorySet();
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_CurtainWallPanels));
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_GenericModel));
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_CurtainWallMullions));
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_Windows));
                    ParameterHelper.RawCreateProjectParameter(doc.Application, "安装开始", ParameterType.Text, true, _catset, BuiltInParameterGroup.PG_PHASING, true);
                }
                #endregion

                #region 设置项目参数：安裝結束
                if (!Global.DocContent.ParameterInfoList.Exists(x => x.Name == "安装结束"))
                {
                    CategorySet _catset = new CategorySet();
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_CurtainWallPanels));
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_GenericModel));
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_CurtainWallMullions));
                    _catset.Insert(doc.Settings.Categories.get_Item(BuiltInCategory.OST_Windows));
                    ParameterHelper.RawCreateProjectParameter(doc.Application, "安装结束", ParameterType.Text, true, _catset, BuiltInParameterGroup.PG_PHASING, true);
                }
                #endregion
                Global.DocContent.ParameterInfoList = ParameterHelper.RawGetProjectParametersInfo(doc);
                trans.Commit();

            }
            #endregion
        }

        #endregion

        //public static void IsolateCategoriesAndZoom(ElementId eid, UIApplication uiapp) {}
    }

    #endregion

}
