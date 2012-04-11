namespace NBehave.Narrator.Framework.Processors
{
    public interface IContextHandler
    {
        void OnParsedTagEvent(string tag);
        void OnFeatureStartedEvent(Feature feature);
        void OnFeatureFinishedEvent();
        void OnScenarioStartedEvent(Scenario scenario);
        void OnScenarioFinishedEvent();
        void OnStepStartedEvent(StringStep step);
        void OnStepFinishedEvent();
    }
}