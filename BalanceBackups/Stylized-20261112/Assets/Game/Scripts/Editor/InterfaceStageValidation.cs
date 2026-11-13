// The original Stage 7 flow was superseded by the fixed-equipment corrections.
namespace Bellator.Editor
{
    public static class InterfaceStageValidation
    {
        [UnityEditor.MenuItem("Bellator/Test Stage 7 Complete Interface in Play Mode")]
        public static void Run() => CorrectionsValidation.Run();
    }
}
