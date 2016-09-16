using Bib3;
using Bib3.Geometrik;
using BotEngine.Motor;
using Sanderling.Interface.MemoryStruct;
using Sanderling.Motor;
using Sanderling.Parse;
using System.Collections.Generic;
using System.Linq;

namespace Sanderling.ABot.Bot
{
	static public class BotExtension
	{
		static readonly EWarTypeEnum[][] listEWarPriorityGroup = new[]
		{
			new[] { EWarTypeEnum.ECM },
			new[] { EWarTypeEnum.Web},
			new[] { EWarTypeEnum.WarpDisrupt, EWarTypeEnum.WarpScramble },
		};

		static public int AttackPriorityIndexForOverviewEntryEWar(IEnumerable<EWarTypeEnum> setEWar)
		{
			var setEWarRendered = setEWar?.ToArray();

			return
				listEWarPriorityGroup.FirstIndexOrNull(priorityGroup => priorityGroup.ContainsAny(setEWarRendered)) ??
				(listEWarPriorityGroup.Length + (0 < setEWarRendered?.Length ? 0 : 1));
		}

		static public int AttackPriorityIndex(
			this Bot bot,
			Sanderling.Parse.IOverviewEntry entry) =>
			AttackPriorityIndexForOverviewEntryEWar(bot?.OverviewMemory?.SetEWarTypeFromOverviewEntry(entry));

		static public bool ShouldBeIncludedInStepOutput(this IBotTask task) =>
			null != task?.Motion;

		static public MotionParam MouseClickOnGameViewport(
			this Interface.MemoryStruct.IMemoryMeasurement memoryMeasurement,
			Vektor2DInt location,
			MouseButtonIdEnum mouseButton) =>
			MouseClickOnGameViewport(memoryMeasurement, location, mouseButton, new Vektor2DInt(8, 8));

			static public MotionParam MouseClickOnGameViewport(
				this Interface.MemoryStruct.IMemoryMeasurement memoryMeasurement,
				Vektor2DInt location,
				MouseButtonIdEnum mouseButton,
				Vektor2DInt	regionSize)
		{
			var topmostUIElement =
				memoryMeasurement.EnumerateReferencedUIElementTransitive()
				?.OrderByDescending(uiElement => uiElement?.InTreeIndex ?? -1)?.FirstOrDefault();

			//	tell the API we click on the topmost UIElement because we do not care about occlusion.
			return topmostUIElement.WithRegion(RectInt.FromCenterAndSize(location, regionSize)).MouseClick(mouseButton);
		}
	}
}
