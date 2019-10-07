using System;
using System.Collections.Generic;
using System.Text;

namespace GraphWorkflow.Net
{
    public class PlaceDiff
    {
        public int PlaceIndex { get; private set; }
        public PlaceState PreviousState { get; private set; }
        public PlaceState NewState { get; private set; }
        public PlaceDiff(int placeIndex, PlaceState previousState, PlaceState newState)
        {
            PlaceIndex = placeIndex;
            PreviousState = previousState;
            NewState = newState;
        }

        public override string ToString()
        {
            return $"Place at index {PlaceIndex} changed from {PreviousState} to {NewState}";
        }
    }
}
