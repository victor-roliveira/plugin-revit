using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using YourNamespace;

[Transaction(TransactionMode.Manual)]
public class SheetComposerCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        try
        {
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;

            // Mostrar a janela WPF
            var window = new SheetComposerWindow(doc);
            if (window.ShowDialog() == true)
            {
                TaskDialog.Show("Sucesso", "Pranchas criadas com sucesso!");
                return Result.Succeeded;
            }

            return Result.Cancelled;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            TaskDialog.Show("Erro", ex.Message);
            return Result.Failed;
        }
    }
}