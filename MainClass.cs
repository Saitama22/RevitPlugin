using System;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;

namespace RevitPlugin
{
	[Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class MainClass : IExternalCommand
    {
		private const string ParamLevelName = "Уровень";
		private const string ParamSectionName = "BS_Блок";
		private const string ParamRoomCountName = "ROM_Подзона";
		private const string ParamZoneName = "ROM_Зона";
		private const string ParamTypeName = "ROM_Расчетная_подзона_ID";
		private const string ParamColorName = "ROM_Подзона_Index";
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
			UIApplication uiapp = commandData.Application;
			Document doc = uiapp.ActiveUIDocument.Document;

			var docElements = new FilteredElementCollector(doc, doc.ActiveView.Id).
			   Cast<Element>().
			   ToList();
			var allRooms = docElements.Where(r => r is Room && r.LookupParameter(ParamZoneName).AsValueString().Contains("Квартира"));
			var groupRooms = allRooms.GroupBy(r => new 
			{ 
				level = r.LookupParameter(ParamLevelName).AsValueString(), 
				section = r.LookupParameter(ParamSectionName).AsValueString(),
				roomCount = r.LookupParameter(ParamRoomCountName).AsValueString(),
			});
			using (Transaction t = new Transaction(doc, "Set Color"))
			{
				t.Start();
				foreach (var groupRoom in groupRooms)
				{
					try
					{
						var flats = groupRoom.Select(r => r).GroupBy(r => int.Parse(r.LookupParameter(ParamZoneName).AsValueString().Replace("Квартира", "")));
						if (flats.Count() <= 1)
							continue;
						var flatsOrdered = flats.OrderBy(r => r.Key).ToArray();
						for (int i = 1; i < flatsOrdered.Count(); i++)
						{
							if (flatsOrdered[i].Key - 1 == flatsOrdered[i - 1].Key)
							{
								var rooms = flatsOrdered[i].Select(r => r);
								var type = rooms.FirstOrDefault().LookupParameter(ParamTypeName).AsValueString();
								foreach (var room in rooms)
								{
									var colorParam = room.LookupParameter(ParamColorName);
									colorParam.Set(type + ".Полутон");
								}
								i++;
							}
						}
					}
					catch (Exception)
					{
						
					}
				}
				t.Commit();
			}
			return Result.Succeeded;
        }
    }
}
