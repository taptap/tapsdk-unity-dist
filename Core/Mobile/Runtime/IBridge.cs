using System;

namespace TapSDK.Core
{
    public interface IBridge
    {
        void Register(string serviceClzName, string serviceImplName);

        void Call(Command command);

        void Call(Command command, Action<Result> action);
        string CallWithReturnValue(Command command, Action<Result> action);
        
    }
}