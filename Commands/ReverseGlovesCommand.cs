using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace primeiro_plugin2.Commands
{
    using Autodesk.Revit.Attributes;
    using Autodesk.Revit.DB;
    using Autodesk.Revit.DB.Mechanical;
    using Autodesk.Revit.DB.Plumbing;
    using Autodesk.Revit.UI;
    using Autodesk.Revit.UI.Selection;
    using System;
    using System.Linq;

    [Transaction(TransactionMode.Manual)]
    public class ReverseGlovesCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            UIDocument uidoc = uiApp.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                // Seleciona um elemento no modelo
                Reference pickedRef = uidoc.Selection.PickObject(ObjectType.Element, "Selecione uma luva para inverter");
                Element element = doc.GetElement(pickedRef);

                // Verifica se o elemento selecionado é um fitting (luva)
                if (element is FamilyInstance fitting && fitting.MEPModel is MechanicalFitting)
                {
                    using (Transaction trans = new Transaction(doc, "Inverter sentido da luva"))
                    {
                        trans.Start();

                        // Obtém os conectores da luva
                        ConnectorSet connectors = GetConnectors(fitting);
                        if (connectors != null && connectors.Size == 2)
                        {
                            Connector[] connArray = connectors.Cast<Connector>().ToArray();
                            XYZ dir1 = connArray[0].CoordinateSystem.BasisZ;
                            XYZ dir2 = connArray[1].CoordinateSystem.BasisZ;

                            // Verifica se já está invertida e troca as direções
                            if (dir1.IsAlmostEqualTo(-dir2))
                            {
                                connArray[0].Origin = connArray[1].Origin;
                                connArray[1].Origin = connArray[0].Origin;
                            }
                        }

                        trans.Commit();
                    }
                    return Result.Succeeded;
                }
                else
                {
                    TaskDialog.Show("Erro", "O elemento selecionado não é uma luva.");
                    return Result.Failed;
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private ConnectorSet GetConnectors(FamilyInstance instance)
        {
            MEPModel mepModel = instance.MEPModel;
            if (mepModel != null)
            {
                return mepModel.ConnectorManager.Connectors;
            }
            return null;
        }
    }

}
