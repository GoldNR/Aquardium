using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Aquardium;

public class StatusUpdateMessage : ValueChangedMessage<(string ArduinoId, string Status)>
{
    public StatusUpdateMessage(string arduinoId, string status)
    : base((arduinoId, status)) {}
}

public class TemperatureUpdateMessage : ValueChangedMessage<(string ArduinoId, string Temperature)>
{
    public TemperatureUpdateMessage(string arduinoId, string temperature)
    : base((arduinoId, temperature)) {}
}