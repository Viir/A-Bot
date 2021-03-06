﻿using Bib3;
using Sanderling.Parse;
using BotEngine.Interface;
using System.Linq;
using System.Collections.Generic;
using System;
using Sanderling.Motor;
using Sanderling.ABot.Bot.Task;
using Sanderling.ABot.Bot.Memory;
using Sanderling.ABot.Serialization;

namespace Sanderling.ABot.Bot
{
	public class Bot
	{
		public BotStepInput StepLastInput { private set; get; }

		BotStepResult stepLastResult;

		int motionId;

		int stepIndex;

		public FromProcessMeasurement<IMemoryMeasurement> MemoryMeasurementAtTime { private set; get; }

		readonly public Accumulator.MemoryMeasurementAccumulator MemoryMeasurementAccu = new Accumulator.MemoryMeasurementAccumulator();

		readonly public OverviewMemory OverviewMemory = new OverviewMemory();

		readonly IDictionary<Int64, int> MouseClickLastStepIndexFromUIElementId = new Dictionary<Int64, int>();

		readonly IDictionary<Accumulation.IShipUiModule, int> ToggleLastStepIndexFromModule = new Dictionary<Accumulation.IShipUiModule, int>();

		public KeyValuePair<Deserialization, Config> ConfigSerialAndStruct { private set; get; }

		public Int64? MouseClickLastAgeStepCountFromUIElement(Interface.MemoryStruct.IUIElement uiElement)
		{
			if (null == uiElement)
				return null;

			var interactionLastStepIndex = MouseClickLastStepIndexFromUIElementId?.TryGetValueNullable(uiElement.Id);

			return stepIndex - interactionLastStepIndex;
		}

		public Int64? ToggleLastAgeStepCountFromModule(Accumulation.IShipUiModule module) =>
			stepIndex - ToggleLastStepIndexFromModule?.TryGetValueNullable(module);

		IEnumerable<IBotTask[]> StepOutputListTaskPath() =>
			((IBotTask)new BotTask { Component = RootTaskListComponent() })
			?.EnumeratePathToNodeFromTreeDFirst(node => node?.Component)
			?.Where(taskPath => (taskPath?.LastOrDefault()).ShouldBeIncludedInStepOutput())
			?.Take(1);

		void MemorizeStepInput(BotStepInput input)
		{
			ConfigSerialAndStruct = input?.ConfigSerial?.String?.DeserializeIfDifferent(ConfigSerialAndStruct) ?? ConfigSerialAndStruct;

			MemoryMeasurementAtTime = input?.FromProcessMemoryMeasurement?.MapValue(measurement => measurement?.Parse());

			MemoryMeasurementAccu.Accumulate(MemoryMeasurementAtTime);

			OverviewMemory.Aggregate(MemoryMeasurementAtTime);
		}

		void MemorizeStepResult(BotStepResult stepResult)
		{
			var setMotionMouseWaypointUIElement =
				stepResult?.ListMotion
				?.Select(motion => motion?.MotionParam)
				?.Where(motionParam => 0 < motionParam?.MouseButton?.Count())
				?.Select(motionParam => motionParam?.MouseListWaypoint)
				?.ConcatNullable()?.Select(mouseWaypoint => mouseWaypoint?.UIElement)?.WhereNotDefault();

			foreach (var mouseWaypointUIElement in setMotionMouseWaypointUIElement.EmptyIfNull())
				MouseClickLastStepIndexFromUIElementId[mouseWaypointUIElement.Id] = stepIndex;
		}

		public BotStepResult Step(BotStepInput input)
		{
			StepLastInput = input;

			Exception exception = null;

			var listMotion = new List<MotionRecommendation>();

			try
			{
				MemorizeStepInput(input);

				var outputListTaskPath = StepOutputListTaskPath()?.ToArray();

				foreach (var moduleToggle in outputListTaskPath.ConcatNullable().OfType<ModuleToggleTask>().Select(moduleToggleTask => moduleToggleTask?.module).WhereNotDefault())
					ToggleLastStepIndexFromModule[moduleToggle] = stepIndex;

				foreach (var taskPath in outputListTaskPath.EmptyIfNull())
				{
					var taskMotionParam = taskPath?.LastOrDefault()?.Motion;

					if (null == taskMotionParam)
						continue;

					listMotion.Add(new MotionRecommendation
					{
						Id = motionId++,
						MotionParam = taskMotionParam,
					});
				}
			}
			catch (Exception e)
			{
				exception = e;
			}

			var stepResult = stepLastResult = new BotStepResult
			{
				Exception = exception,
				ListMotion = listMotion?.ToArrayIfNotEmpty(),
			};

			MemorizeStepResult(stepResult);

			++stepIndex;

			return stepResult;
		}

		IEnumerable<IBotTask> RootTaskListComponent()
		{
			yield return new EnableInfoPanelCurrentSystem { MemoryMeasurement = MemoryMeasurementAtTime?.Value };

			var saveShipTask = new SaveShipTask { Bot = this };

			yield return saveShipTask;

			yield return this.EnsureIsActive(MemoryMeasurementAccu?.ShipUiModule?.Where(module => module?.TooltipLast?.Value?.IsHardener ?? false));

			var moduleUnknown = MemoryMeasurementAccu?.ShipUiModule?.FirstOrDefault(module => null == module?.TooltipLast?.Value);

			yield return new BotTask { Motion = moduleUnknown?.MouseMove() };

			if (!saveShipTask.AllowRoam)
				yield break;

			var combatTask = new CombatTask { bot = this };

			yield return combatTask;

			if (!saveShipTask.AllowAnomalyEnter)
				yield break;

			yield return new UndockTask { MemoryMeasurement = MemoryMeasurementAtTime?.Value };

			if (combatTask.Completed)
				yield return new AnomalyEnter { bot = this };
		}
	}
}
