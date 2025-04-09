using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;

namespace primeiro_plugin2.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class CreateView : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;

            try
            {
                using (Transaction trans = new Transaction(doc, "Criar Prancha"))
                {
                    trans.Start();

                    CriarPranchaComPlanta(doc);

                    trans.Commit();
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private void CriarPranchaComPlanta(Document doc)
        {
            ViewSheet sheet = ViewSheet.Create(doc, GetTitleBlockId(doc));
            ViewPlan viewPlan = GetFirstFloorPlan(doc);

            if (viewPlan != null)
            {
                UV location = new UV((sheet.Outline.Max.U - sheet.Outline.Min.U) / 2,
                            (sheet.Outline.Max.V - sheet.Outline.Min.V) / 2);

                string sheetNumber = GenerateSheetNumber(doc);
                string sheetName = "PLANTA BAIXA - " + viewPlan.Name;

                sheet.SheetNumber = sheetNumber;
                sheet.Name = sheetName;
            }
        }
        // Método auxiliar para obter o TitleBlock padrão
        private ElementId GetTitleBlockId(Document doc)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfCategory(BuiltInCategory.OST_TitleBlocks);
            collector.WhereElementIsElementType();

            // Retorna o primeiro TitleBlock encontrado (você pode querer filtrar por nome/tipo)
            return collector.FirstElementId();
        }

        // Método auxiliar para obter a primeira vista de planta baixa
        private ViewPlan GetFirstFloorPlan(Document doc)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(ViewPlan));

            foreach (ViewPlan view in collector)
            {
                if (view.ViewType == ViewType.FloorPlan && !view.IsTemplate)
                {
                    return view;
                }
            }

            return null;
        }

        // Método auxiliar para gerar número de prancha sequencial
        private string GenerateSheetNumber(Document doc)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            collector.OfClass(typeof(ViewSheet));

            // Lógica simples para gerar número sequencial (A101, A102, etc.)
            return "A" + (collector.GetElementCount() + 101);
        }
    }
}
