using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using primeiro_plugin2.Commands;
using System.Reflection;

namespace primeiro_plugin2
{
    public class UIPlugin : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            // Criar aba e painel (como antes)
            string tabName = "Meu Plugin";
            try { application.CreateRibbonTab(tabName); } catch { }

            RibbonPanel panel = application.CreateRibbonPanel(tabName, "Ferramentas");

            // === Botão 1 (existente) ===
            PushButtonData btn1Data = new PushButtonData(
                "Btn1",
                "Selecionar Elemento",
                Assembly.GetExecutingAssembly().Location,
                typeof(SelectionCommand).FullName
            );
            panel.AddItem(btn1Data);

            // === Botão 2 (novo) ===
            PushButtonData btn2Data = new PushButtonData(
                "Btn2",
                "Contar Paredes",
                Assembly.GetExecutingAssembly().Location,
                typeof(CountWallsCommand).FullName
            );
            panel.AddItem(btn2Data);

            // === Botão 3 (novo) ===
            PushButtonData btn3Data = new PushButtonData(
                "Btn3",
                "Inverter Luvas",
                Assembly.GetExecutingAssembly().Location,
                typeof(ReverseGlovesCommand).FullName
            );
            panel.AddItem(btn3Data);
            
            // === Botão 4 (novo) ===
            PushButtonData btn4Data = new PushButtonData(
                "Btn4",
                "Gerar Prancha",
                Assembly.GetExecutingAssembly().Location,
                typeof(SheetComposerCommand).FullName
            );
            panel.AddItem(btn4Data);

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}
