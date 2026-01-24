# Class Relationships

Total classes: 61

## RCDragManagerProd.Program

- File: `RCDragManagerProd\Program.cs`
- Composes:
  - RCDragManagerProd.UI.Forms.LandingForm

## RCDragManagerProd.ViewModels.MatchResultSave

- File: `RCDragManagerProd\ViewModels\MatchResultSave.cs`

## RCDragManagerProd.ViewModels.PairingRow

- File: `RCDragManagerProd\ViewModels\PairingRow.cs`

## RCDragManagerProd.ViewModels.RaceSessionSummary

- File: `RCDragManagerProd\ViewModels\RaceSessionSummary.cs`

## RCDragManagerProd.ViewModels.WinnerRow

- File: `RCDragManagerProd\ViewModels\WinnerRow.cs`

## RCDragManagerProd.DicEx.DictEx

- File: `RCDragManagerProd\Utils\DictEx.cs`

## RCDragManagerProd.UI.Forms.LandingForm

- File: `RCDragManagerProd\UI\Forms\Session\LandingPageForm.Designer.cs`
- Base: `Form`
- Composes:
  - RCDragManagerProd.Repositories.DriverRepository
  - RCDragManagerProd.Repositories.RaceSessionRepository
  - RCDragManagerProd.Domain.RaceSession
  - RCDragManagerProd.Controllers.RaceController
  - RCDragManagerProd.UI.Forms.Form1
  - RCDragManagerProd.UI.Forms.SessionSetupForm
  - RCDragManagerProd.UI.Forms.LoadSessionForm
  - RCDragManagerProd.UI.Forms.DriverManagerForm
  - RCDragManagerProd.UI.Forms.SettingsForm

## RCDragManagerProd.UI.Forms.LoadSessionForm

- File: `RCDragManagerProd\UI\Forms\Session\LoadSessionForm.Designer.cs`
- Base: `Form`
- Composes:
  - RCDragManagerProd.Repositories.RaceSessionRepository
  - RCDragManagerProd.ViewModels.RaceSessionSummary
  - RCDragManagerProd.Domain.RaceSession

## RCDragManagerProd.UI.Forms.SessionSetupForm

- File: `RCDragManagerProd\UI\Forms\Session\SessionSetupForm.UI.cs`
- Base: `Form`
- Composes:
  - RCDragManagerProd.Repositories.DriverRepository
  - RCDragManagerProd.Domain.RaceSession
  - RCDragManagerProd.UI.Forms.AddDriverAndCarDialog

## RCDragManagerProd.UI.Forms.BuybackDriverSelectionForm

- File: `RCDragManagerProd\UI\Forms\Results\BuybackDriverSelectionForm.Designer.cs`
- Base: `Form`
- Composes:
  - RCDragManagerProd.Domain.Driver

## RCDragManagerProd.UI.Forms.EditWinnerDialog

- File: `RCDragManagerProd\UI\Forms\Results\EditWinnerDialog.Designer.cs`
- Base: `Form`
- Composes:
  - RCDragManagerProd.Domain.Driver

## RCDragManagerProd.UI.Forms.Form1

- File: `RCDragManagerProd\UI\Forms\Main\Form1.WinnerButtons.cs`
- Base: `Form`
- Composes:
  - RCDragManagerProd.Controllers.RaceController
  - RCDragManagerProd.ViewModels.PairingRow
  - RCDragManagerProd.ViewModels.WinnerRow
  - RCDragManagerProd.Controllers.RaceSummary
  - RCDragManagerProd.RaceEngines.EngineMatch
  - RCDragManagerProd.Domain.Driver
  - RCDragManagerProd.Domain.RaceSession
  - RCDragManagerProd.Repositories.RaceSessionRepository
  - RCDragManagerProd.Repositories.DriverRepository
  - RCDragManagerProd.UI.Forms.EditDriverDialog
  - RCDragManagerProd.UI.Forms.AddEditQualTimeDialog
  - RCDragManagerProd.UI.Forms.BuybackDriverSelectionForm

## RCDragManagerProd.UI.Forms.AddDriverAndCarDialog

- File: `RCDragManagerProd\UI\Forms\Drivers\AddDriverAndCarDialog.Designer.cs`
- Base: `Form`

## RCDragManagerProd.UI.Forms.AddDriverDialog

- File: `RCDragManagerProd\UI\Forms\Drivers\AddDriverDialog.Designer.cs`
- Base: `Form`

## RCDragManagerProd.UI.Forms.AddEditQualTimeDialog

- File: `RCDragManagerProd\UI\Forms\Drivers\AddEditQualTimeDialog.Designer.cs`
- Base: `Form`

## RCDragManagerProd.UI.Forms.DriverManagerForm

- File: `RCDragManagerProd\UI\Forms\Drivers\DriverManagerForm.UI.cs`
- Base: `Form`
- Composes:
  - RCDragManagerProd.Repositories.DriverRepository
  - RCDragManagerProd.Domain.Driver
  - RCDragManagerProd.UI.Forms.AddDriverAndCarDialog
  - RCDragManagerProd.UI.Forms.EditDriverDialog
  - RCDragManagerProd.UI.Forms.AddCarDialog
  - RCDragManagerProd.UI.Forms.AddEditQualTimeDialog
  - RCDragManagerProd.UI.Forms.DriverStatsForm

## RCDragManagerProd.UI.Forms.DriverStatsForm

- File: `RCDragManagerProd\UI\Forms\Drivers\DriverStatsForm.Designer.cs`
- Base: `Form`
- Composes:
  - RCDragManagerProd.Domain.Driver
  - RCDragManagerProd.Repositories.RaceSessionRepository

## RCDragManagerProd.UI.Forms.EditDriverDialog

- File: `RCDragManagerProd\UI\Forms\Drivers\EditDriverDialog.Designer.cs`
- Base: `Form`

## RCDragManagerProd.UI.Forms.ScrollableTextDialog

- File: `RCDragManagerProd\UI\Forms\Common\ScrollableTextDialog.cs`
- Base: `Form`

## RCDragManagerProd.UI.Forms.SettingsForm

- File: `RCDragManagerProd\UI\Forms\Common\SettingsForm.cs`
- Base: `Form`

## RCDragManagerProd.UI.Forms.AddCarDialog

- File: `RCDragManagerProd\UI\Forms\Cars\AddCarDialog.Designer.cs`
- Base: `Form`
- Composes:
  - RCDragManagerProd.Domain.Car

## RCDragManagerProd.UI.Forms.SelectCarDialog

- File: `RCDragManagerProd\UI\Forms\Cars\SelectCarDialog.Designer.cs`
- Base: `Form`
- Composes:
  - RCDragManagerProd.Domain.Car

## RCDragManagerProd.RoundRobinMode.RoundRobinEngine

- File: `RCDragManagerProd\RoundRobinMode\RoundRobinEngine.cs`
- Composes:
  - RCDragManagerProd.Domain.Driver
  - RoundRobinMatch
  - RCDragManagerProd.Domain.MatchResult
  - RCDragManagerProd.RoundRobinMode.RoundRobinRanker

## RoundRobinMatch

- File: `RCDragManagerProd\RoundRobinMode\RoundRobinMatch.cs`
- Composes:
  - RCDragManagerProd.Domain.Driver

## RCDragManagerProd.RoundRobinMode.DriverRankResult

- File: `RCDragManagerProd\RoundRobinMode\RoundRobinRanker.cs`

## RCDragManagerProd.RoundRobinMode.RoundRobinRanker

- File: `RCDragManagerProd\RoundRobinMode\RoundRobinRanker.cs`
- Composes:
  - RCDragManagerProd.RoundRobinMode.Aggregate

## RCDragManagerProd.RoundRobinMode.Aggregate

- File: `RCDragManagerProd\RoundRobinMode\RoundRobinRanker.cs`

## RCDragManagerProd.RoundRobinMode.RoundRobinScorecardLogger

- File: `RCDragManagerProd\RoundRobinMode\RoundRobinScorecardLogger\RoundRobinScorecardWriter.cs`
- Composes:
  - RCDragManagerProd.RaceEngines.RoundRobinEngineAdapter
  - RCDragManagerProd.Domain.MatchResult

## RCDragManagerProd.RoundRobinMode.Line

- File: `RCDragManagerProd\RoundRobinMode\RoundRobinScorecardLogger\RoundRobinScorecardLogger.cs`
- Composes:
  - RCDragManagerProd.RaceEngines.RoundRobinEngineAdapter
  - RCDragManagerProd.Domain.MatchResult
  - RCDragManagerProd.RaceEngines.EngineMatch

## RCDragManagerProd.Repositories.CarRepository

- File: `RCDragManagerProd\Repositories\CarRepository.cs`
- Composes:
  - RCDragManagerProd.Domain.Car

## RCDragManagerProd.Repositories.DatabaseInitializer

- File: `RCDragManagerProd\Repositories\DatabaseInitializer.cs`

## RCDragManagerProd.Repositories.DriverRepository

- File: `RCDragManagerProd\Repositories\DriverRepository.cs`
- Composes:
  - RCDragManagerProd.Domain.Driver
  - RCDragManagerProd.Domain.Car

## RCDragManagerProd.Repositories.RaceSessionRepository

- File: `RCDragManagerProd\Repositories\RaceSessionRepository.cs`
- Composes:
  - RCDragManagerProd.ViewModels.RaceSessionSummary
  - RCDragManagerProd.Domain.RaceSession

## RCDragManagerProd.RandomMode.LosersBracketBuilder

- File: `RCDragManagerProd\RandomMode\LosersBracketBuilder.cs`

## RCDragManagerProd.RandomMode.LosersBracketEngine

- File: `RCDragManagerProd\RandomMode\LosersBracketEngine.cs`

## RCDragManagerProd.RandomMode.RandomBracket

- File: `RCDragManagerProd\RandomMode\RandomBracket.cs`
- Composes:
  - RCDragManagerProd.RandomMode.RandomMatch
  - RCDragManagerProd.Domain.Driver

## RCDragManagerProd.RandomMode.RandomMatch

- File: `RCDragManagerProd\RandomMode\RandomMatch.cs`
- Composes:
  - RCDragManagerProd.Domain.Driver

## RCDragManagerProd.RandomMode.RandomMatchEngine

- File: `RCDragManagerProd\RandomMode\RandomMatchEngine.cs`
- Composes:
  - RCDragManagerProd.RandomMode.RandomMatch
  - RCDragManagerProd.Domain.Driver
  - RCDragManagerProd.Domain.MatchResult

## RCDragManagerProd.RaceEngines.EngineMatch

- File: `RCDragManagerProd\RaceEngines\IRaceEngine.cs`
- Composes:
  - RCDragManagerProd.Domain.Driver

## RCDragManagerProd.RaceEngines.MatchEngine

- File: `RCDragManagerProd\RaceEngines\MatchEngine.cs`
- Composes:
  - RCDragManagerProd.Domain.Driver
  - RCDragManagerProd.Domain.LadderMatch
  - RCDragManagerProd.Domain.MatchResult

## RCDragManagerProd.RaceEngines.ProLadderEngineAdapter

- File: `RCDragManagerProd\RaceEngines\ProLadderEngineAdapter.cs`
- Base: `IRaceEngine`
- Composes:
  - RCDragManagerProd.Domain.Driver
  - RCDragManagerProd.RaceEngines.EngineMatch
  - RCDragManagerProd.Domain.LadderMatch
  - RCDragManagerProd.RaceEngines.MatchEngine

## RCDragManagerProd.RaceEngines.RaceEngineFactory

- File: `RCDragManagerProd\RaceEngines\RaceEngineFactory.cs`
- Composes:
  - RCDragManagerProd.RaceEngines.ProLadderEngineAdapter
  - RCDragManagerProd.RaceEngines.RoundRobinEngineAdapter
  - RCDragManagerProd.RaceEngines.RandomEngineAdapter

## RCDragManagerProd.RaceEngines.RandomEngineAdapter

- File: `RCDragManagerProd\RaceEngines\RandomEngineAdapter.cs`
- Base: `IRaceEngine`
- Composes:
  - RCDragManagerProd.RandomMode.RandomMatchEngine
  - RCDragManagerProd.Domain.Driver
  - RCDragManagerProd.RaceEngines.EngineMatch
  - RCDragManagerProd.RandomMode.RandomMatch

## RCDragManagerProd.RaceEngines.RoundRobinEngineAdapter

- File: `RCDragManagerProd\RaceEngines\RoundRobinEngineAdapter.cs`
- Base: `IRaceEngine`
- Composes:
  - RCDragManagerProd.Domain.Driver
  - RCDragManagerProd.RaceEngines.EngineMatch
  - RCDragManagerProd.RoundRobinMode.RoundRobinEngine

## RCDragManagerProd.Properties.Resources

- File: `RCDragManagerProd\Properties\Resources.Designer.cs`

## RCDragManagerProd.Properties.Settings

- File: `RCDragManagerProd\Properties\Settings.Designer.cs`
- Base: `global`

## RCDragManagerProd.Logging.Logger

- File: `RCDragManagerProd\Logging\Logger.cs`

## RCDragManagerProd.Helpers.AssetPath

- File: `RCDragManagerProd\Helpers\AssetPath.cs`

## RCDragManagerProd.Helpers.MatchLookupHelper

- File: `RCDragManagerProd\Helpers\MatchLookupHelper.cs`

## RCDragManagerProd.Domain.Car

- File: `RCDragManagerProd\Domain\Car.cs`

## RCDragManagerProd.Domain.Driver

- File: `RCDragManagerProd\Domain\Drivers.cs`
- Composes:
  - RCDragManagerProd.Domain.Car

## RCDragManagerProd.Domain.MatchResult

- File: `RCDragManagerProd\Domain\MatchResult.cs`
- Composes:
  - RCDragManagerProd.Domain.Driver
  - RCDragManagerProd.Domain.LadderMatch

## RCDragManagerProd.Domain.ProLadder

- File: `RCDragManagerProd\Domain\Ladders\ProLadder.Ladders.L24.cs`
- Composes:
  - RCDragManagerProd.Domain.LadderMatch

## RCDragManagerProd.Domain.LadderMatch

- File: `RCDragManagerProd\Domain\ProLadder.Structures.cs`

## RCDragManagerProd.Domain.RaceSession

- File: `RCDragManagerProd\Domain\RaceSession.cs`
- Composes:
  - RCDragManagerProd.Domain.RaceSessionDriverEntry
  - RCDragManagerProd.ViewModels.MatchResultSave
  - RCDragManagerProd.Domain.MatchResultSave
  - RoundRobinMatch
  - RCDragManagerProd.RandomMode.RandomMatch
  - RCDragManagerProd.Domain.Driver

## RCDragManagerProd.Domain.RaceSessionDriverEntry

- File: `RCDragManagerProd\Domain\RaceSession.cs`

## RCDragManagerProd.Domain.MatchResultSave

- File: `RCDragManagerProd\Domain\RaceSession.cs`

## RCDragManagerProd.Controllers.RaceController

- File: `RCDragManagerProd\Controllers\RaceController.Session.cs`
- Composes:
  - RCDragManagerProd.RaceEngines.RoundRobinEngineAdapter
  - RCDragManagerProd.Domain.Driver
  - RCDragManagerProd.RaceEngines.EngineMatch
  - RCDragManagerProd.ViewModels.PairingRow
  - RCDragManagerProd.Domain.RaceSession
  - RCDragManagerProd.ViewModels.WinnerRow
  - RCDragManagerProd.Domain.MatchResult
  - RCDragManagerProd.RaceEngines.ProLadderEngineAdapter
  - RCDragManagerProd.RaceEngines.RandomEngineAdapter

## RCDragManagerProd.Controllers.RaceSummary

- File: `RCDragManagerProd\Controllers\RaceController.cs`
- Composes:
  - RCDragManagerProd.Domain.RaceSession
  - RCDragManagerProd.Domain.Driver
  - RCDragManagerProd.RaceEngines.EngineMatch

## RCDragManagerProd.Config.AppSettings

- File: `RCDragManagerProd\Config\AppSettings.cs`

## RCDragManagerProd.Config.Model

- File: `RCDragManagerProd\Config\AppSettings.cs`

