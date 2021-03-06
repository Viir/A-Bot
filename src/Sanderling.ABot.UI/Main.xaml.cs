﻿using BotEngine.Interface;
using BotEngine.UI;
using Sanderling.UI;
using System.Windows.Controls;

namespace Sanderling.ABot.UI
{
	public partial class Main : UserControl
	{
		public void BotMotionDisable() => ToggleButtonMotionEnable?.LeftButtonDown();

		public void BotMotionEnable() => ToggleButtonMotionEnable?.RightButtonDown();

		public bool IsBotMotionEnabled => ToggleButtonMotionEnable?.ButtonRecz?.IsChecked ?? false;

		public void ConfigFromModelToView(ExeConfig config) =>
			Interface.LicenseView?.LicenseClientConfigViewModel?.PropagateFromClrMemberToDependencyProperty(config?.LicenseClient?.CompletedWithDefault());

		public ExeConfig ConfigFromViewToModel() =>
			new ExeConfig()
			{
				LicenseClient = Interface.LicenseView?.LicenseClientConfigViewModel?.PropagateFromDependencyPropertyToClrMember(),
			};

		public Main()
		{
			InitializeComponent();
		}

		public void Present(
			SimpleInterfaceServerDispatcher interfaceServerDispatcher,
			FromProcessMeasurement<Interface.MemoryStruct.IMemoryMeasurement> measurement)
		{
			Interface?.Present(interfaceServerDispatcher, measurement);

			InterfaceHeader?.SetStatus(Interface.InterfaceStatusEnum());
		}
	}
}
