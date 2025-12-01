public class GameStatePauseService : IPauseService
{
    private readonly GameStateManager gameStateManager;

    public GameStatePauseService(GameStateManager gameStateManager)
    {
        this.gameStateManager = gameStateManager;
    }

    public void RequestPause(object source)
    {
        gameStateManager?.RequestPause(source);
    }

    public void ReleasePause(object source)
    {
        gameStateManager?.ReleasePause(source);
    }
}
