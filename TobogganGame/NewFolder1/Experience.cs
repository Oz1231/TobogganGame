using System;

namespace TobogganGame
{
    /// <summary>
    /// A record of a state-action-reward transition for Q-learning
    /// </summary>
    /// 
    [Serializable]
    public class Experience
    {
        public double[] State { get; set; }
        public int Action { get; set; }
        public double Reward { get; set; }
        public double[] NextState { get; set; }
        public bool IsDone { get; set; }

        /// <summary>
        /// Creates a new experience record
        /// </summary>
        public Experience(double[] state, int action, double reward, double[] nextState, bool isDone)
        {
            State = (double[])state.Clone();
            Action = action;
            Reward = reward;
            NextState = (double[])nextState.Clone();
            IsDone = isDone;
        }
    }
}