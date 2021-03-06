// Copyright (c) Microsoft Corporation.  All Rights Reserved.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.FSharp.Editor

open System
open System.Composition
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks
open System.Linq

open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.Editor.Implementation.Debugging
open Microsoft.CodeAnalysis.Host.Mef
open Microsoft.CodeAnalysis.Text
open Microsoft.CodeAnalysis.ExternalAccess.FSharp.Editor.Implementation.Debugging

open FSharp.Compiler.SourceCodeServices
open FSharp.Compiler.Range

[<Export(typeof<IFSharpBreakpointResolutionService>)>]
type internal FSharpBreakpointResolutionService 
    [<ImportingConstructor>]
    (
        checkerProvider: FSharpCheckerProvider,
        projectInfoManager: FSharpProjectOptionsManager
    ) =

    static let userOpName = "BreakpointResolution"
    static member GetBreakpointLocation(checker: FSharpChecker, sourceText: SourceText, fileName: string, textSpan: TextSpan, parsingOptions: FSharpParsingOptions) = 
        async {
            let textLinePos = sourceText.Lines.GetLinePosition(textSpan.Start)
            let textInLine = sourceText.GetSubText(sourceText.Lines.[textLinePos.Line].Span).ToString()

            if String.IsNullOrWhiteSpace textInLine then
                return None
            else
                let textLineColumn = textLinePos.Character
                let fcsTextLineNumber = Line.fromZ textLinePos.Line // Roslyn line numbers are zero-based, FSharp.Compiler.Service line numbers are 1-based
                let! parseResults = checker.ParseFile(fileName, sourceText.ToFSharpSourceText(), parsingOptions, userOpName = userOpName)
                return parseResults.ValidateBreakpointLocation(mkPos fcsTextLineNumber textLineColumn)
        }

    interface IFSharpBreakpointResolutionService with
        member this.ResolveBreakpointAsync(document: Document, textSpan: TextSpan, cancellationToken: CancellationToken): Task<FSharpBreakpointResolutionResult> =
            asyncMaybe {
                let! parsingOptions, _options = projectInfoManager.TryGetOptionsForEditingDocumentOrProject(document, cancellationToken)
                let! sourceText = document.GetTextAsync(cancellationToken)
                let! range = FSharpBreakpointResolutionService.GetBreakpointLocation(checkerProvider.Checker, sourceText, document.Name, textSpan, parsingOptions)
                let! span = RoslynHelpers.TryFSharpRangeToTextSpan(sourceText, range)
                return FSharpBreakpointResolutionResult.CreateSpanResult(document, span)
            } 
            |> Async.map Option.toObj 
            |> RoslynHelpers.StartAsyncAsTask cancellationToken
            
        // FSROSLYNTODO: enable placing breakpoints by when user supplies fully-qualified function names
        member this.ResolveBreakpointsAsync(_, _, _): Task<IEnumerable<FSharpBreakpointResolutionResult>> =
            Task.FromResult(Enumerable.Empty<FSharpBreakpointResolutionResult>())