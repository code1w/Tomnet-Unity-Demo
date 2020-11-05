/*
 * This file is part of the TomNet package.
 *
 * (a) zhang xiao bin <qunshuok@gmail.com>
 *
 *  2020/09/10
 */

namespace TomNet.Core
{
    public interface IDispatchable
    {
        EventDispatcher Dispatcher
        {
            get;
        }

        void AddEventListener(string eventType, EventListenerDelegate listener);
    }
}
