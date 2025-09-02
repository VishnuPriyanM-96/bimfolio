using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MultipleParameterNested
{
    [Transaction(TransactionMode.Manual)]
    public class PropagateParameterCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                // Pick element
                Reference pickedRef = uidoc.Selection.PickObject(ObjectType.Element, "Select a family instance");
                Element el = doc.GetElement(pickedRef);

                if (!(el is FamilyInstance famInst))
                {
                    TaskDialog.Show("Error", "Please select a family instance.");
                    return Result.Failed;
                }

                // Collect parameters
                IList<Parameter> parameters = famInst.Parameters.Cast<Parameter>().ToList();

                // Build preview list for UI
                var paramList = parameters
                    .Select(p => new ParamPreview
                    {
                        Name = p.Definition.Name,
                        ValuePreview = GetParamPreview(p)
                    })
                    .ToList();

                // Show WPF window for parameter selection
                ParameterPicker picker = new ParameterPicker(paramList);
                bool? dialogResult = picker.ShowDialog();

                if (dialogResult != true || picker.SelectedParam == null)
                {
                    TaskDialog.Show("Cancelled", "Operation cancelled by user.");
                    return Result.Cancelled;
                }

                string selectedParamName = picker.SelectedParam.Name;
                Parameter sourceParam = famInst.LookupParameter(selectedParamName);

                if (sourceParam == null)
                {
                    TaskDialog.Show("Error", "Selected parameter not found on element.");
                    return Result.Failed;
                }

                // Copy value to nested families
                using (Transaction tx = new Transaction(doc, "Propagate Parameter"))
                {
                    tx.Start();

                    foreach (ElementId subId in famInst.GetSubComponentIds())
                    {
                        Element subEl = doc.GetElement(subId);
                        Parameter targetParam = subEl.LookupParameter(selectedParamName);

                        if (targetParam != null)
                        {
                            SetParameterValue(targetParam, sourceParam);
                        }
                    }

                    tx.Commit();
                }

                TaskDialog.Show("Success", $"Parameter '{selectedParamName}' propagated to nested families.");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private static bool SetParameterValue(Parameter target, Parameter source)
        {
            try
            {
                switch (target.StorageType)
                {
                    case StorageType.String:
                        return target.Set(source.AsString());
                    case StorageType.Integer:
                        return target.Set(source.AsInteger());
                    case StorageType.Double:
                        return target.Set(source.AsDouble());
                    case StorageType.ElementId:
                        return target.Set(source.AsElementId());
                    default:
                        return false;
                }
            }
            catch { return false; }
        }

        private static string GetParamPreview(Parameter p)
        {
            try
            {
                switch (p.StorageType)
                {
                    case StorageType.String:
                        return p.AsString();
                    case StorageType.Integer:
                        return p.AsInteger().ToString();
                    case StorageType.Double:
                        return p.AsDouble().ToString("F2");
                    case StorageType.ElementId:
                        return p.AsElementId().IntegerValue.ToString();
                    default:
                        return "<n/a>";
                }
            }
            catch
            {
                return "<error>";
            }
        }
    }

    // Helper class for binding to WPF
    public class ParamPreview
    {
        public string Name { get; set; }
        public string ValuePreview { get; set; }
    }
}
