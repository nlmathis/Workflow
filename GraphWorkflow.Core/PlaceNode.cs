using System;
using System.Collections.Generic;
using System.Text;

namespace GraphWorkflow.Core
{
    public class PlaceNode
    {
        public bool HasToken { get; set; }
        public PlaceNode(bool hasToken)
        {
            HasToken = hasToken;
        }
    }
}
