using System;
using System.Collections.Generic;
using System.Text;

namespace SharedKernel
{


    public class Event
    {
        public string Name
        {
            get { return GetType().Name; }
        }

        public virtual void EnumerateProperties(Action<(string name, string content)> propertyCallback)
        {
        }
    }


}

namespace System.Runtime.CompilerServices
{
    public class IsExternalInit { } // Dummy to prevent preview bug
}