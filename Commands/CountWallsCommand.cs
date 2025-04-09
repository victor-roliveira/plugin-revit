using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Linq;

namespace primeiro_plugin2.Commands
{
    [Transaction(TransactionMode.ReadOnly)]
    public class CountWallsCommand
    {
        [Transaction(TransactionMode.Manual)]
        public class SecondCommand : IExternalCommand
        {
            public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
            {
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                Document doc = uidoc.Document;

                try
                {
                    var walls = new FilteredElementCollector(doc)
                        .OfClass(typeof(Wall))
                        .Count();

                    TaskDialog.Show("Total de paredes: ", walls.ToString());

                    return Result.Succeeded;
                }
                catch
                {
                    return Result.Failed;
                }
            }
        }
    }
}
