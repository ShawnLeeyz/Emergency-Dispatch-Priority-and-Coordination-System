using System;
using System.Collections.Generic;
using System.Text;

/*
 * The dispatcher class is a domain knowledge of all the attributes of the dispatcher, which includes
 * (Id, Name, IsActive)
 */
namespace Emergency_Dispatch_Priority_and_Coordination_System.Domain
{
    internal class Dispatcher
    {
        // Attributes of the dispatcher are defined here
        public string dispatcherId { get; private set; } = Guid.NewGuid().ToString();
        public string name { get; private set; }
        public bool isActive { get; set; } = true;

        public Dispatcher(string name)
        {
            this.name = name;
        }
    }
}