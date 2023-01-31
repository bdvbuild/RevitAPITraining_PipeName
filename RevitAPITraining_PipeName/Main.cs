using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Transaction = Autodesk.Revit.DB.Transaction;

namespace RevitAPITraining_PipeName
{
    [Transaction(TransactionMode.Manual)]
    public class Main : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            var categorySet = new CategorySet();
            categorySet.Insert(Category.GetCategory(doc, BuiltInCategory.OST_PipeCurves));

            using (Transaction ts = new Transaction(doc, "Add parameter"))
            {
                ts.Start();
                CreateSharedParameter(uiapp.Application, doc, "Наименование", categorySet, BuiltInParameterGroup.PG_DATA, true);
                ts.Commit();
            }

            List<Pipe> pipes = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_PipeCurves)
                .WhereElementIsNotElementType()
                .Cast<Pipe>()
                .ToList();

            foreach (var elem in pipes)
            {
                Parameter pipeName = elem.LookupParameter("Наименование");
                Parameter outerDiameter = elem.get_Parameter(BuiltInParameter.RBS_PIPE_OUTER_DIAMETER);
                Parameter innerDiameter = elem.get_Parameter(BuiltInParameter.RBS_PIPE_INNER_DIAM_PARAM);

                string outer = outerDiameter.AsValueString();
                string inner = innerDiameter.AsValueString();
                string result = $"Труба {outer}/{inner}";

                using (Transaction ts = new Transaction(doc, "Set parameter"))
                {
                    ts.Start();
                    pipeName.Set(result);
                    ts.Commit();
                }
            }
            return Result.Succeeded;
        }

        private void CreateSharedParameter(
            Application application,
            Document doc,
            string parameterName,
            CategorySet categorySet,
            BuiltInParameterGroup builtInParameterGroup,
            bool isInstance)
        {
            DefinitionFile definitionFile = application.OpenSharedParameterFile();
            if (definitionFile == null)
            {
                TaskDialog.Show("Ошибка", "Не найден файл общих параметров");
                return;
            }

            Definition definition = definitionFile.Groups
                .SelectMany(group => group.Definitions)
                .FirstOrDefault(def => def.Name.Equals(parameterName));
            if (definition == null)
            {
                TaskDialog.Show("Ошибка", "Не найден указанный параметр");
                return;
            }

            Binding binding = application.Create.NewTypeBinding(categorySet);
            if (isInstance)
                binding = application.Create.NewInstanceBinding(categorySet);

            BindingMap bindingMap = doc.ParameterBindings;
            bindingMap.Insert(definition, binding, builtInParameterGroup);
        }
    }
}
