using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace YourNamespace
{
    public partial class SheetComposerWindow : Window
    {
        private Document _document;
        private Dictionary<Border, View> _viewElements = new Dictionary<Border, View>();
        private System.Windows.Point _dragStartPoint;
        private Border _draggedElement;
        private View _draggedView;

        public SheetComposerWindow(Document doc)
        {
            InitializeComponent(); // Agora esta linha funcionará
            _document = doc;
            LoadAvailableViews();
            LoadExistingSheets();
        }

        private void LoadAvailableViews()
        {
            FilteredElementCollector collector = new FilteredElementCollector(_document);
            collector.OfClass(typeof(View));

            List<View> views = new List<View>();
            foreach (View view in collector)
            {
                if (!view.IsTemplate && view.CanBePrinted)
                {
                    // Verifica se é uma vista que queremos incluir
                    if (view.ViewType == ViewType.FloorPlan ||   // Plantas
                        view.ViewType == ViewType.EngineeringPlan ||
                        view.ViewType == ViewType.Section ||     // Cortes
                        view.ViewType == ViewType.Elevation)    // Elevações
                    {
                        views.Add(view);
                    }
                }
            }

            AvailableViewsList.ItemsSource = views;
        }

        private void LoadExistingSheets()
        {
            FilteredElementCollector collector = new FilteredElementCollector(_document);
            collector.OfClass(typeof(ViewSheet));
            SheetsComboBox.ItemsSource = collector.ToElements();
        }

        private void AvailableViews_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (AvailableViewsList.SelectedItem is View selectedView)
            {
                _draggedView = selectedView;

                // Inicia a operação de arrastar
                DataObject dragData = new DataObject("REVIT_VIEW", selectedView);
                DragDrop.DoDragDrop(AvailableViewsList, dragData, DragDropEffects.Copy);

                _draggedView = null; // Limpa após a operação
            }
        }

        private void SheetCompositionArea_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("REVIT_VIEW"))
            {
                e.Effects = DragDropEffects.Copy;

                // Adiciona feedback visual durante o arrasto
                var border = sender as Border;
                if (border != null)
                {
                    border.Background = Brushes.LightBlue;
                }
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void SheetCompositionArea_DragLeave(object sender, DragEventArgs e)
        {
            // Restaura a cor original quando o cursor sai
            var border = sender as Border;
            if (border != null)
            {
                border.Background = Brushes.LightGray;
            }
        }

        private void SheetCompositionArea_Drop(object sender, DragEventArgs e)
        {
            var border = sender as Border;
            if (border != null)
            {
                border.Background = Brushes.LightGray; // Restaura a cor original
            }

            if (e.Data.GetData("REVIT_VIEW") is View view)
            {
                System.Windows.Point dropPosition = e.GetPosition(SheetCompositionArea);

                var viewFrame = CreateViewFrame(view);
                Canvas.SetLeft(viewFrame, dropPosition.X - viewFrame.Width / 2);
                Canvas.SetTop(viewFrame, dropPosition.Y - viewFrame.Height / 2);

                SheetCompositionArea.Children.Add(viewFrame);
                _viewElements.Add(viewFrame, view);
            }
        }

        private Border CreateViewFrame(View view)
        {
            var viewFrame = new Border
            {
                Style = (Style)FindResource("ViewFrameStyle"),
                Width = 180,
                Height = 120,
                Child = new TextBlock
                {
                    Text = view.Name,
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(5),
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold
                },
                Tag = view.Id,
                ToolTip = $"Tipo: {view.ViewType}\nEscala: 1:{view.Scale}"
            };

            // Configura eventos para mover
            viewFrame.MouseLeftButtonDown += ViewFrame_MouseLeftButtonDown;
            viewFrame.MouseMove += ViewFrame_MouseMove;
            viewFrame.MouseLeftButtonUp += ViewFrame_MouseLeftButtonUp;

            return viewFrame;
        }

        private void ViewFrame_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _draggedElement = sender as Border;
            if (_draggedElement != null)
            {
                _dragStartPoint = e.GetPosition(SheetCompositionArea);
                _draggedElement.CaptureMouse();
                e.Handled = true;
            }
        }

        private void ViewFrame_MouseMove(object sender, MouseEventArgs e)
        {
            if (_draggedElement != null && e.LeftButton == MouseButtonState.Pressed)
            {
                System.Windows.Point currentPosition = e.GetPosition(SheetCompositionArea);
                double offsetX = currentPosition.X - _dragStartPoint.X;
                double offsetY = currentPosition.Y - _dragStartPoint.Y;

                Canvas.SetLeft(_draggedElement, Canvas.GetLeft(_draggedElement) + offsetX);
                Canvas.SetTop(_draggedElement, Canvas.GetTop(_draggedElement) + offsetY);

                _dragStartPoint = currentPosition;
            }
        }

        private void ViewFrame_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_draggedElement != null)
            {
                _draggedElement.ReleaseMouseCapture();
                _draggedElement = null;
            }
        }

        private void SheetCompositionArea_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _draggedElement = null;
            SheetCompositionArea.ReleaseMouseCapture();
        }

        private void CreateNewSheet_Click(object sender, RoutedEventArgs e)
        {
            SheetCompositionArea.Children.Clear();
            _viewElements.Clear();
        }

        private void GenerateSheets_Click(object sender, RoutedEventArgs e)
        {
            List<ViewPlacement> placements = new List<ViewPlacement>();

            foreach (var kvp in _viewElements)
            {
                placements.Add(new ViewPlacement
                {
                    ViewId = kvp.Value.Id,
                    X = Canvas.GetLeft(kvp.Key),
                    Y = Canvas.GetTop(kvp.Key)
                });
            }

            SheetCreator.CreateSheets(_document, placements);
            this.DialogResult = true;
            Close();
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

        private void SheetCompositionArea_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("REVIT_VIEW"))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }
    }

    public class ViewPlacement
    {
        public ElementId ViewId { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
    }
}