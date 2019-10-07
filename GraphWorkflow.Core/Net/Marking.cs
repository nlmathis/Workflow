using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GraphWorkflow.Net
{
    public class Marking
    {
        public IList<Place> Places = new List<Place>();
        public IList<Transition> Transitions = new List<Transition>();
       

        public Marking()
        {

        }

        public Marking(IList<Place> places, IList<Transition> transitions)
        {
            Places = places;
            Transitions = transitions;
        }

        public Marking Clone()
        {
            var clonedMarking = new Marking(Places.Select(place => place.Clone()).ToList(),
                Transitions.Select(transition => transition.Clone()).ToList());

            return clonedMarking;
        }

        public IEnumerable<Transition> GetEnabledTransitions()
        {
            return Transitions.Where(transition => transition.InputPlaceIndicies.All(inputPlaceIndex => Places[inputPlaceIndex].State == PlaceState.HasToken));
        }

        public IEnumerable<PlaceDiff> GetPlaceDiffs(Marking previousMarking)
        {
            var diffs = previousMarking.Places
                .Zip(Places, (prevPlace, currPlace) => new { prevPlace, currPlace })
                .Select((placePair, placeIndex) => new { placeIndex, placePair.prevPlace, placePair.currPlace })
                .Where(placePair => placePair.prevPlace.State != placePair.currPlace.State)
                .Select(placePair => new PlaceDiff(placePair.placeIndex, placePair.prevPlace.State, placePair.currPlace.State));

            return diffs;
        }

        public IEnumerable<TransitionDiff> GetTransitionDiffs(Marking previousMarking)
        {
           var diffs =
               previousMarking.Transitions
               .Zip(Transitions, (prevTransition, currTransition) => new { prevTransition, currTransition })
               .Select((transitionPair, transitionIndex) => new { transitionIndex, transitionPair.prevTransition, transitionPair.currTransition })
               .Where(transitionPair => transitionPair.prevTransition.State != transitionPair.currTransition.State)
               .Select(transitionPair => new TransitionDiff(transitionPair.transitionIndex, transitionPair.prevTransition.State, transitionPair.currTransition.State));

            return diffs;
        }
    }
}
