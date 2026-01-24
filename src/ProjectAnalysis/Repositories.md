# Repositories and SQL Usage

Total repositories: 4

## RCDragManagerProd.Repositories.CarRepository

- File: `RCDragManagerProd\Repositories\CarRepository.cs`
- SQL usages: 4

- [RCDragManagerProd\Repositories\CarRepository.cs:79] GetCarsByDriver : SELECT `SELECT CarID, CarName, ClassType, DefaultDialIn FROM Cars WHERE DriverId = @DriverId`
  - Tables: Cars
  - Columns: CarID, CarName, ClassType, DefaultDialIn
  - Params: @DriverId
- [RCDragManagerProd\Repositories\CarRepository.cs:104] UpdateCar : UPDATE `[DB][CarRepo] UpdateCar(CarID={car.CarID}, Name='{car.CarName}')`
- [RCDragManagerProd\Repositories\CarRepository.cs:124] DeleteCar : DELETE `[DB][CarRepo] DeleteCar(CarID={carId})`
- [RCDragManagerProd\Repositories\CarRepository.cs:126] DeleteCar : DELETE `DELETE FROM Cars WHERE CarID = @CarID`
  - Tables: Cars
  - Params: @CarID

## RCDragManagerProd.Repositories.DatabaseInitializer

- File: `RCDragManagerProd\Repositories\DatabaseInitializer.cs`
- SQL usages: 0


## RCDragManagerProd.Repositories.DriverRepository

- File: `RCDragManagerProd\Repositories\DriverRepository.cs`
- SQL usages: 15

- [RCDragManagerProd\Repositories\DriverRepository.cs:57] GetAllDrivers : SELECT `SELECT * FROM Drivers`
  - Tables: Drivers
  - Columns: *
- [RCDragManagerProd\Repositories\DriverRepository.cs:90] GetDriverById : SELECT `SELECT * FROM Drivers WHERE Id = @Id`
  - Tables: Drivers
  - Columns: *
  - Params: @Id
- [RCDragManagerProd\Repositories\DriverRepository.cs:157] UpdateDriver : UPDATE `[DB][DriverRepo] UpdateDriver(Id={driver.Id}, Name='{driver.Name}')`
- [RCDragManagerProd\Repositories\DriverRepository.cs:187] UpdateDriver : DELETE `DELETE FROM Cars WHERE DriverId = @DriverId`
  - Tables: Cars
  - Params: @DriverId
- [RCDragManagerProd\Repositories\DriverRepository.cs:200] UpdateDriver : UPDATE `[DB][DriverRepo] UpdateDriver → OK`
- [RCDragManagerProd\Repositories\DriverRepository.cs:205] DeleteDriver : DELETE `[DB][DriverRepo] DeleteDriver(Id={id})`
- [RCDragManagerProd\Repositories\DriverRepository.cs:209] DeleteDriver : DELETE `DELETE FROM Cars WHERE DriverId = @DriverId`
  - Tables: Cars
  - Params: @DriverId
- [RCDragManagerProd\Repositories\DriverRepository.cs:215] DeleteDriver : DELETE `DELETE FROM Drivers WHERE Id = @Id`
  - Tables: Drivers
  - Params: @Id
- [RCDragManagerProd\Repositories\DriverRepository.cs:222] DeleteDriver : DELETE `[DB][DriverRepo] DeleteDriver → OK`
- [RCDragManagerProd\Repositories\DriverRepository.cs:258] GetCarsByDriverId : SELECT `SELECT * FROM Cars WHERE DriverId = @DriverId`
  - Tables: Cars
  - Columns: *
  - Params: @DriverId
- [RCDragManagerProd\Repositories\DriverRepository.cs:283] UpdateQualifyingTime : UPDATE `[DB][DriverRepo] UpdateQualifyingTime(id={driverId}, time={qualTime})`
- [RCDragManagerProd\Repositories\DriverRepository.cs:288] UpdateQualifyingTime : UPDATE `UPDATE Drivers SET QualTime = @QualTime WHERE Id = @Id`
  - Params: @QualTime, @Id
- [RCDragManagerProd\Repositories\DriverRepository.cs:294] UpdateQualifyingTime : UPDATE `[DB][DriverRepo] UpdateQualifyingTime → OK`
- [RCDragManagerProd\Repositories\DriverRepository.cs:300] ComputeEventsWonFromSavedSessions : `[STATS] ComputeEventsWonFromSavedSessions: driverId={driverId}`
- [RCDragManagerProd\Repositories\DriverRepository.cs:306] ComputeEventsWonFromSavedSessions : SELECT `SELECT SessionData FROM RaceSessions`
  - Tables: RaceSessions
  - Columns: SessionData

## RCDragManagerProd.Repositories.RaceSessionRepository

- File: `RCDragManagerProd\Repositories\RaceSessionRepository.cs`
- SQL usages: 4

- [RCDragManagerProd\Repositories\RaceSessionRepository.cs:133] LoadSession : SELECT `SELECT SessionData FROM RaceSessions WHERE Id = @Id`
  - Tables: RaceSessions
  - Columns: SessionData
  - Params: @Id
- [RCDragManagerProd\Repositories\RaceSessionRepository.cs:165] DeleteSession : DELETE `[DB][SessionRepo] DeleteSession(id={id})`
- [RCDragManagerProd\Repositories\RaceSessionRepository.cs:167] DeleteSession : DELETE `DELETE FROM RaceSessions WHERE Id = @Id`
  - Tables: RaceSessions
  - Params: @Id
- [RCDragManagerProd\Repositories\RaceSessionRepository.cs:172] DeleteSession : DELETE `[DB][SessionRepo] DeleteSession → OK`

