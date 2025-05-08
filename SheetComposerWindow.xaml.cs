using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace YourNamespace
{
    public partial class SheetComposerWindow : Window
    {
        private Document _document;
        private Dictionary<Border, View> _viewElements = new Dictionary<Border, View>();

        // Classe para representar as vistas no ListBox
        public class ViewItem
        {
            public string Name { get; set; }
            public ElementId Id { get; set; }
            public ViewType Type { get; set; }
            public bool IsSelected { get; set; } // Adicionado para a seleção via CheckBox
        }

        public SheetComposerWindow(Document doc)
        {
            InitializeComponent();
            _document = doc;
            LoadAvailableViews();
        }

        private void LoadAvailableViews()
        {
            try
            {
                // Coletar todas as vistas do documento
                FilteredElementCollector collector = new FilteredElementCollector(_document);
                collector.OfClass(typeof(View));

                // Filtrar e preparar as vistas para exibição
                var views = new List<ViewItem>();
                foreach (View view in collector)
                {
                    if (!view.IsTemplate && view.CanBePrinted)
                    {
                        // Filtrar por tipos de vista específicos
                        if (view.ViewType == ViewType.FloorPlan ||
                            view.ViewType == ViewType.EngineeringPlan ||
                            view.ViewType == ViewType.Section ||
                            view.ViewType == ViewType.Elevation)
                        {
                            views.Add(new ViewItem
                            {
                                Name = view.Name,
                                Id = view.Id,
                                Type = view.ViewType,
                                IsSelected = false // Inicialmente não selecionado
                            });
                        }
                    }
                }

                // Ordenar as vistas por nome
                ViewsList.ItemsSource = views.OrderBy(v => v.Name);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar vistas: {ex.Message}", "Erro",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GenerateSheets_Click(object sender, RoutedEventArgs e)
        {
            var selectedViews = ViewsList.ItemsSource
                .Cast<ViewItem>()
                .Where(v => v.IsSelected)
                .ToList();

            if (!selectedViews.Any())
            {
                MessageBox.Show("Selecione pelo menos uma vista.", "Aviso",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ElementId titleBlockId = SheetCreator.GetTitleBlockId(_document);

            if (SingleSheetRadio.IsChecked == true)
            {
                // Modo: Uma única prancha com múltiplas vistas (escala reduzida)
                var placements = CalculateViewPlacementsForSingleSheet(selectedViews, titleBlockId);
                SheetCreator.CreateSingleSheet(_document, placements, titleBlockId);
            }
            else
            {
                // Modo: Uma prancha por vista (escala original)
                foreach (var viewItem in selectedViews)
                {
                    var placement = new ViewPlacement
                    {
                        ViewId = viewItem.Id,
                        ShouldCenter = true // Usando a nova propriedade
                    };
                    SheetCreator.CreateSheetForView(_document, placement, titleBlockId);
                }
            }

            this.DialogResult = true;
            Close();
        }

        private List<ViewPlacement> CalculateViewPlacementsForSingleSheet(List<ViewItem> selectedViews, ElementId titleBlockId)
        {
            var placements = new List<ViewPlacement>();
            const double sheetWidth = 841;  // Largura A1 em mm
            const double sheetHeight = 594; // Altura A1 em mm

            // Margens otimizadas para A1
            const double topMargin = 15;    // Margem superior
            const double bottomMargin = 25; // Margem inferior
            const double sideMargin = 35;   // Margem lateral
            const double rowSpacing = 40;   // Aumentado de 20 para 30mm (ajuste solicitado)

            const int fixedScale = 200;     // Escala fixa 1:200
            const int columns = 2;          // 2 colunas

            // Cálculo do espaço útil
            double usableHeight = sheetHeight - topMargin - bottomMargin;
            double cellHeight = (usableHeight - rowSpacing) / 2;
            double cellWidth = (sheetWidth - (2 * sideMargin)) / columns;

            for (int i = 0; i < selectedViews.Count; i++)
            {
                int row = i / columns;
                int col = i % columns;

                // Cálculo da posição Y com novo espaçamento
                double yPos = topMargin + (row * (cellHeight + rowSpacing)) + (cellHeight / 2);

                // Verificação do limite inferior
                if (yPos + (cellHeight / 2) > sheetHeight - bottomMargin)
                {
                    MessageBox.Show($"Limite máximo de {i} vistas na prancha A1", "Aviso",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
                }

                double xPos = sideMargin + (col * cellWidth) + (cellWidth / 2);

                placements.Add(new ViewPlacement
                {
                    ViewId = selectedViews[i].Id,
                    X = xPos,
                    Y = yPos,
                    ScaleFactor = fixedScale,
                    ViewWidth = cellWidth * 0.85,
                    ViewHeight = cellHeight * 0.85
                });
            }

            return placements;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            Close();
        }

        private void AvailableViewsList_PreviewGiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            e.UseDefaultCursors = false;
            Mouse.SetCursor(Cursors.Cross);
            e.Handled = true;
        }
    }

    public class ViewPlacement
    {
        public ElementId ViewId { get; set; }
        public double X { get; set; }        // Posição X em mm (centro)
        public double Y { get; set; }        // Posição Y em mm (centro)
        public int? ScaleFactor { get; set; } // Escala (ex: 200)
        public double? ViewWidth { get; set; }  // Largura máxima em mm
        public double? ViewHeight { get; set; } // Altura máxima em mm
        public bool ShouldCenter { get; set; } // Nova propriedade para centralização
    }
}