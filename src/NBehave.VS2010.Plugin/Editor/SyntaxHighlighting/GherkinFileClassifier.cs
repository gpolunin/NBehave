﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Disposables;
using System.Linq;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using NBehave.VS2010.Plugin.Editor.Extensions;
using NBehave.VS2010.Plugin.Editor.SyntaxHighlighting.Classifiers;

namespace NBehave.VS2010.Plugin.Editor.SyntaxHighlighting
{

    [Export(typeof(IClassifierProvider))]
    [ContentType("gherkin")]
    internal class GherkinFileClassifierProvider : IClassifierProvider
    {
        [Import]
        public ServiceRegistrar ServiceRegistrar { get; set; }

        public IClassifier GetClassifier(ITextBuffer buffer)
        {
            ServiceRegistrar.Initialise(buffer);
            
            return buffer.Properties.GetProperty<IClassifier>(typeof(GherkinFileClassifier));
        }
    }

    [Export(typeof(GherkinFileClassifier))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class GherkinFileClassifier : IClassifier, IDisposable
    {
        private GherkinFileEditorParser _parser;
        private List<ClassificationSpan> _spans;
        private CompositeDisposable _listeners;

        public GherkinFileClassifier(ITextBuffer buffer)
        {
            _spans = new List<ClassificationSpan>();
            _listeners = new CompositeDisposable();

            _parser = buffer.Properties.GetProperty<GherkinFileEditorParser>(typeof (GherkinFileEditorParser));

            _listeners.Add(_parser.IsParsing.Where(isParsing => isParsing).Subscribe(b => _spans.Clear()));
            _listeners.Add(_parser.IsParsing.Where(isParsing => !isParsing).Subscribe(b => PublishClassificationEvents()));

            _listeners.Add(_parser
                            .ParserEvents
                            .Select(parserEvent => Classifiers
                                    .With(list => list.FirstOrDefault(classifier => classifier.CanClassify(parserEvent)))
                                    .Return(gherkinClassifier => gherkinClassifier.Classify(parserEvent), new List<ClassificationSpan>()))
                            .Subscribe((spans => _spans.AddRange(spans))));
        }

        public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;
        
        [ImportMany]
        public IEnumerable<IGherkinClassifier> Classifiers { get; set; }
        
        public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan span)
        {
            return _spans;
        }

        public void BeginClassifications()
        {
            _parser.FirstParse();
        }

        private void PublishClassificationEvents()
        {
            if (ClassificationChanged != null)
            {
                foreach (var classificationSpan in _spans)
                {
                    ClassificationChanged(this, new ClassificationChangedEventArgs(classificationSpan.Span));
                }
            }
        }

        public void Dispose()
        {
            _listeners.Dispose();
        }
    }
}