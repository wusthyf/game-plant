using System;

namespace PlantSpirit.GGJ
{
    public enum GameState
    {
        MainMenu,
        Loading,
        Playing,
        Grafting,
        Paused,
        Dead,
        Result
    }

    public sealed class GameStateController
    {
        public GameState Current { get; private set; } = GameState.MainMenu;
        public event Action<GameState> Changed;

        public bool SetState(GameState next)
        {
            if ((Current == GameState.Dead || Current == GameState.Result) && next != GameState.Loading && next != GameState.MainMenu) return false;
            if ((Current == GameState.Grafting && next == GameState.Paused) || (Current == GameState.Paused && next == GameState.Grafting)) return false;
            if (Current == next) return false;
            Current = next;
            Changed?.Invoke(Current);
            return true;
        }
    }
}
