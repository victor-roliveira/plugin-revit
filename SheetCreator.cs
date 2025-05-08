using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using YourNamespace;


public static class SheetCreator
{
    public static void CreateSingleSheet(Document doc, List<ViewPlacement> placements, ElementId titleBlockId)
    {
        using (Transaction trans = new Transaction(doc, "Criar Prancha A1"))
        {
            trans.Start();

            try
            {
                ViewSheet sheet = ViewSheet.Create(doc, titleBlockId);
                sheet.SheetNumber = GenerateSheetNumber(doc);
                sheet.Name = "Prancha Composta A1";

                var (sheetWidth, sheetHeight) = GetTitleBlockSize(doc, titleBlockId);
                const double standardA1Height = 594;

                foreach (var placement in placements)
                {
                    View view = doc.GetElement(placement.ViewId) as View;
                    if (view == null || !CanAddViewToSheet(view)) continue;

                    try
                    {
                        if (GetSheetContainingView(doc, view.Id) != null)
                        {
                            view = DuplicateView(doc, view) ?? view;
                        }

                        if (placement.ScaleFactor.HasValue)
                        {
                            Parameter scaleParam = view.get_Parameter(BuiltInParameter.VIEW_SCALE_PULLDOWN_METRIC);
                            if (scaleParam != null && scaleParam.StorageType == StorageType.Integer)
                            {
                                scaleParam.Set(placement.ScaleFactor.Value);
                            }
                        }

                        if (view.CropBoxActive && placement.ViewWidth.HasValue && placement.ViewHeight.HasValue)
                        {
                            double maxWidth = placement.ViewWidth.Value / 304.8;
                            double maxHeight = placement.ViewHeight.Value / 304.8;

                            BoundingBoxXYZ crop = view.CropBox;
                            double scaleFactor = Math.Min(
                                maxWidth / (crop.Max.X - crop.Min.X),
                                maxHeight / (crop.Max.Y - crop.Min.Y)
                            ) * 0.90;

                            XYZ newSize = (crop.Max - crop.Min) * scaleFactor;
                            XYZ center = (crop.Max + crop.Min) / 2;

                            view.CropBox = new BoundingBoxXYZ
                            {
                                Min = center - (newSize / 2),
                                Max = center + (newSize / 2)
                            };
                        }

                        double xPos = placement.X / 304.8;
                        double yCorrection = (standardA1Height - sheetHeight) / 2;
                        double yPos = (sheetHeight - placement.Y + yCorrection) / 304.8;

                        Viewport.Create(doc, sheet.Id, view.Id, new XYZ(xPos, yPos, 0));
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Erro ao posicionar vista {view.Name}: {ex.Message}");
                    }
                }

                trans.Commit();
                TaskDialog.Show("Sucesso", "Prancha criada com layout preciso!");
            }
            catch (Exception ex)
            {
                trans.RollBack();
                TaskDialog.Show("Erro", $"Falha na criação: {ex.Message}");
            }
        }
    }

    public static void CreateSheetForView(Document doc, ViewPlacement placement, ElementId titleBlockId)
    {
        using (Transaction trans = new Transaction(doc, "Criar Prancha Individual"))
        {
            trans.Start();

            try
            {
                ViewSheet sheet = ViewSheet.Create(doc, titleBlockId);
                sheet.SheetNumber = GenerateSheetNumber(doc);

                View view = doc.GetElement(placement.ViewId) as View;
                sheet.Name = view?.Name ?? "Prancha Sem Nome";

                if (view != null && CanAddViewToSheet(view))
                {
                    try
                    {
                        // Duplicar vista se necessário
                        if (GetSheetContainingView(doc, view.Id) != null)
                        {
                            view = DuplicateView(doc, view) ?? view;
                        }

                        // Centralizar vista se ShouldCenter for true
                        if (placement.ShouldCenter)
                        {
                            var (sheetWidth, sheetHeight) = GetTitleBlockSize(doc, titleBlockId);
                            double xPos = sheetWidth / 2 / 304.8;  // Converter mm para pés
                            double yPos = sheetHeight / 2 / 304.8;

                            Viewport.Create(doc, sheet.Id, view.Id, new XYZ(xPos, yPos, 0));
                        }
                        else
                        {
                            // Usar posições específicas se fornecidas
                            double xPos = placement.X / 304.8;
                            double yPos = (GetTitleBlockSize(doc, titleBlockId).height - placement.Y) / 304.8;
                            Viewport.Create(doc, sheet.Id, view.Id, new XYZ(xPos, yPos, 0));
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Erro ao adicionar vista: {ex.Message}");
                    }
                }

                trans.Commit();
            }
            catch (Exception ex)
            {
                trans.RollBack();
                throw;
            }
        }
    }

    public static (double width, double height) GetViewSize(View view)
    {
        try
        {
            // Obter o bounding box da vista em pés
            BoundingBoxXYZ bb = view.get_BoundingBox(null);
            if (bb != null && bb.Min != null && bb.Max != null)
            {
                // Calcular tamanho em pés (já considera a escala)
                double widthInFeet = bb.Max.X - bb.Min.X;
                double heightInFeet = bb.Max.Y - bb.Min.Y;

                // Converter para milímetros (1 pé = 304.8 mm)
                double widthInMm = widthInFeet * 304.8;
                double heightInMm = heightInFeet * 304.8;

                // Valores máximos para evitar tamanhos absurdos
                widthInMm = Math.Min(widthInMm, 5000); // Máximo 5 metros
                heightInMm = Math.Min(heightInMm, 5000);

                return (widthInMm, heightInMm);
            }
        }
        catch
        {
            // Se falhar, usar valores padrão
        }

        // Valores padrão baseados no tipo de vista (em mm)
        switch (view.ViewType)
        {
            case ViewType.FloorPlan:
                return (300, 200);
            case ViewType.Section:
                return (200, 300);
            case ViewType.Elevation:
                return (150, 250);
            default:
                return (250, 250);
        }
    }

    public static (double width, double height) GetTitleBlockSize(Document doc, ElementId titleBlockId)
    {
        try
        {
            Element titleBlock = doc.GetElement(titleBlockId);
            if (titleBlock != null)
            {
                BoundingBoxXYZ bb = titleBlock.get_BoundingBox(null);
                if (bb != null && bb.Min != null && bb.Max != null)
                {
                    return (
                        (bb.Max.X - bb.Min.X) * 304.8,
                        (bb.Max.Y - bb.Min.Y) * 304.8
                    );
                }
            }
        }
        catch
        {
            // Fallback para A1 padrão se houver erro
        }
        return (841, 594); // Default A1
    }

    public static bool CanAddViewToSheet(View view)
    {
        if (view == null || view.IsTemplate || !view.CanBePrinted)
            return false;

        // Permitir qualquer escala, pois vamos forçar a escala fixa
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

    public static ElementId GetTitleBlockId(Document doc)
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