﻿using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Windows.Interfaces
{
    interface IClipboardListWindow : ISingleInstance
    {
        void Show();
    }
}