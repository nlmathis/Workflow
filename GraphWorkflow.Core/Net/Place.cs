using System;
using System.Collections.Generic;
using System.Text;

namespace GraphWorkflow.Net
{
    public class Place
    {
        public bool HasToken { get { return State == PlaceState.HasToken; } }
        public PlaceState State { get; set; }

        public Place()
        {
            State = PlaceState.Unused;
        }

        public Place(PlaceState state)
        {
            State = state;
        }

        public Place Clone()
        {
            return new Place(State);
        }
    }

    public enum PlaceState
    {
        HasToken,
        Empty,
        Unused
    }

}
