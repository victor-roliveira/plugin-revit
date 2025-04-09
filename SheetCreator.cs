using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using YourNamespace;


public static class SheetCreator
{
    public static void CreateSheets(Document doc, List<ViewPlacement> placements)
    {
        using (Transaction trans = new Transaction(doc, "Criar Pranchas com Vistas"))
        {
            try
            {
                trans.Start();

                // Criar nova prancha
                ViewSheet sheet = ViewSheet.Create(doc, GetTitleBlockId(doc));
                sheet.SheetNumber = GenerateSheetNumber(doc);
                sheet.Name = "Prancha Composta";

                bool viewsAdded = false;

                foreach (var placement in placements)
                {
                    View view = doc.GetElement(placement.ViewId) as View;

                    if (view != null && CanAddViewToSheet(view))
                    {
                        try
                        {
                            // Verificar se a vista já está em outra prancha
                            if (GetSheetContainingView(doc, view.Id) != null)
                            {
                                // Duplicar a vista dentro da transação existente
                                View duplicatedView = DuplicateView(doc, view);
                                view = duplicatedView ?? view; // Usa a original se não conseguir duplicar
                            }

                            // Posicionamento da vista
                            double x = (placement.X / 304.8) + 1.0;
                            double y = (sheet.Outline.Max.V - (placement.Y / 304.8)) - 1.0;

                            Viewport vp = Viewport.Create(doc, sheet.Id, view.Id, new XYZ(x, y, 0));

                            if (vp != null)
                            {
                                viewsAdded = true;
                                Debug.WriteLine($"Viewport criado para {view.Name}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Erro ao adicionar vista: {ex.Message}");
                        }
                    }
                }

                if (viewsAdded)
                {
                    trans.Commit();
                    Debug.WriteLine("Prancha criada com sucesso");
                }
                else
                {
                    trans.RollBack();
                    Debug.WriteLine("Nenhuma vista foi adicionada");
                }
            }
            catch (Exception ex)
            {
                trans.RollBack();
                Debug.WriteLine($"Erro crítico: {ex.Message}");
                throw;
            }
        }
    }

    // para debug apenas
    private static void LogPositionInfo(Document doc, ViewSheet sheet, XYZ location, View view)
    {
        Debug.WriteLine($"Informações de Posicionamento para {view.Name}:");
        Debug.WriteLine($"Posição calculada: X={location.X}, Y={location.Y}");
        Debug.WriteLine($"Escala da vista: 1:{view.Scale}");

        BoundingBoxUV outline = sheet.Outline;
        Debug.WriteLine($"Limites da prancha: Min(U={outline.Min.U}, V={outline.Min.V}) | Max(U={outline.Max.U}, V={outline.Max.V})");

        // Verificação mais robusta para TitleBlock
        var titleBlocks = new FilteredElementCollector(doc, sheet.Id)
            .OfCategory(BuiltInCategory.OST_TitleBlocks)
            .WhereElementIsNotElementType()
            .ToElements();

        if (titleBlocks.Any())
        {
            Element titleBlock = titleBlocks.First();
            BoundingBoxXYZ bb = titleBlock.get_BoundingBox(sheet);
            if (bb != null)
            {
                Debug.WriteLine($"Área útil (title block): Min(X={bb.Min.X}, Y={bb.Min.Y}) | Max(X={bb.Max.X}, Y={bb.Max.Y})");
            }
        }
        else
        {
            Debug.WriteLine("Nenhum title block encontrado na prancha - usando outline completo");
        }
    }

    private static bool IsWithinSheetBounds(ViewSheet sheet, XYZ location)
    {
        try
        {
            // Obter os limites considerando escala
            BoundingBoxUV outline = sheet.Outline;
            double margin = 1.0; // Margem maior para escala 1:100

            return location.X > (outline.Min.U + margin) &&
                   location.X < (outline.Max.U - margin) &&
                   location.Y > (outline.Min.V + margin) &&
                   location.Y < (outline.Max.V - margin);
        }
        catch
        {
            return false;
        }
    }

    private static bool CanAddViewToSheet(View view)
    {
        if (view == null || view.IsTemplate || !view.CanBePrinted)
            return false;

        // Garantir que a vista tenha escala adequada
        if (view.Scale < 50 || view.Scale > 200) // Limites razoáveis para 1:100
        {
            TaskDialog.Show("Aviso", $"A vista '{view.Name}' tem escala 1:{view.Scale} - ajuste para entre 1:50 e 1:200");
            return false;
        }

        return view.ViewType == ViewType.FloorPlan ||
               view.ViewType == ViewType.Section ||
               view.ViewType == ViewType.Elevation;
    }

    private static ViewSheet GetSheetContainingView(Document doc, ElementId viewId)
    {
        FilteredElementCollector collector = new FilteredElementCollector(doc);
        collector.OfClass(typeof(Viewport));

        foreach (Viewport vp in collector)
        {
            if (vp.ViewId == viewId)
            {
                return doc.GetElement(vp.SheetId) as ViewSheet;
            }
        }
        return null;
    }

    private static View DuplicateView(Document doc, View originalView)
    {
        // Verificar se podemos iniciar uma nova transação
        if (doc.IsModifiable == false || doc.IsReadOnly)
        {
            Debug.WriteLine("Documento não está disponível para modificação");
            return null;
        }

        try
        {
            // Usar a transação principal existente em vez de criar uma nova
            if (originalView != null && originalView.IsValidObject)
            {
                // Duplicar como independente
                ElementId newViewId = originalView.Duplicate(ViewDuplicateOption.Duplicate);
                return doc.GetElement(newViewId) as View;
            }
            return null;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Erro ao duplicar vista: {ex.Message}");
            return null;
        }
    }

    private static ElementId GetTitleBlockId(Document doc)
    {
        FilteredElementCollector collector = new FilteredElementCollector(doc);
        collector.OfCategory(BuiltInCategory.OST_TitleBlocks);
        collector.WhereElementIsElementType();
        return collector.FirstElementId();
    }

    private static string GenerateSheetNumber(Document doc)
    {
        FilteredElementCollector collector = new FilteredElementCollector(doc);
        collector.OfClass(typeof(ViewSheet));
        return "A" + (collector.GetElementCount() + 101);
    }
}