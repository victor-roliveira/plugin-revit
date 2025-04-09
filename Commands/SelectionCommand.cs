using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI.Selection;
using System;

namespace primeiro_plugin2.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class SelectionCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                // 1. Solicitar ao usuário que ele selecione um elemento
                Reference pickedRef = uidoc.Selection.PickObject(ObjectType.Element, "Selecione um elemento na tela");
                if (pickedRef != null)
                {
                    // 2. Obter o elemento selecionado
                    Element element = doc.GetElement(pickedRef);
                    // 3. Extrair nome e família do elemento
                    string elementName = element.Name;
                    string familyName = element.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM).AsValueString();
                    // 4. Exibir informações
                    TaskDialog.Show("Elemento: ", elementName + "\nFamília: " + familyName);
                }
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                // Usuário cancelou a seleção
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }

            return Result.Succeeded;
        }
    }
}