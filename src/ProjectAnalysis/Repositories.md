# Repositories and SQL Usage

Total repositories: 4

## RCDragManagerProd.Repositories.CarRepository

- File: `RCDragManagerProd\Repositories\CarRepository.cs`
- SQL usages: 4

- [RCDragManagerProd\Repositories\CarRepository.cs:79] GetCarsByDriver : `SELECT CarID, CarName, ClassType, DefaultDialIn FROM Cars WHERE DriverId = @DriverId`
- [RCDragManagerProd\Repositories\CarRepository.cs:104] UpdateCar : `[DB][CarRepo] UpdateCar(CarID={car.CarID}, Name='{car.CarName}')`
- [RCDragManagerProd\Repositories\CarRepository.cs:124] DeleteCar : `[DB][CarRepo] DeleteCar(CarID={carId})`
- [RCDragManagerProd\Repositories\CarRepository.cs:126] DeleteCar : `DELETE FROM Cars WHERE CarID = @CarID`

## RCDragManagerProd.Repositories.DatabaseInitializer

- File: `RCDragManagerProd\Repositories\DatabaseInitializer.cs`
- SQL usages: 0


## RCDragManagerProd.Repositories.DriverRepository

- File: `RCDragManagerProd\Repositories\DriverRepository.cs`
- SQL usages: 15

- [RCDragManagerProd\Repositories\DriverRepository.cs:57] GetAllDrivers : `SELECT * FROM Drivers`
- [RCDragManagerProd\Repositories\DriverRepository.cs:90] GetDriverById : `SELECT * FROM Drivers WHERE Id = @Id`
- [RCDragManagerProd\Repositories\DriverRepository.cs:157] UpdateDriver : `[DB][DriverRepo] UpdateDriver(Id={driver.Id}, Name='{driver.Name}')`
- [RCDragManagerProd\Repositories\DriverRepository.cs:187] UpdateDriver : `DELETE FROM Cars WHERE DriverId = @DriverId`
- [RCDragManagerProd\Repositories\DriverRepository.cs:200] UpdateDriver : `[DB][DriverRepo] UpdateDriver → OK`
- [RCDragManagerProd\Repositories\DriverRepository.cs:205] DeleteDriver : `[DB][DriverRepo] DeleteDriver(Id={id})`
- [RCDragManagerProd\Repositories\DriverRepository.cs:209] DeleteDriver : `DELETE FROM Cars WHERE DriverId = @DriverId`
- [RCDragManagerProd\Repositories\DriverRepository.cs:215] DeleteDriver : `DELETE FROM Drivers WHERE Id = @Id`
- [RCDragManagerProd\Repositories\DriverRepository.cs:222] DeleteDriver : `[DB][DriverRepo] DeleteDriver → OK`
- [RCDragManagerProd\Repositories\DriverRepository.cs:258] GetCarsByDriverId : `SELECT * FROM Cars WHERE DriverId = @DriverId`
- [RCDragManagerProd\Repositories\DriverRepository.cs:283] UpdateQualifyingTime : `[DB][DriverRepo] UpdateQualifyingTime(id={driverId}, time={qualTime})`
- [RCDragManagerProd\Repositories\DriverRepository.cs:288] UpdateQualifyingTime : `UPDATE Drivers SET QualTime = @QualTime WHERE Id = @Id`
- [RCDragManagerProd\Repositories\DriverRepository.cs:294] UpdateQualifyingTime : `[DB][DriverRepo] UpdateQualifyingTime → OK`
- [RCDragManagerProd\Repositories\DriverRepository.cs:300] ComputeEventsWonFromSavedSessions : `[STATS] ComputeEventsWonFromSavedSessions: driverId={driverId}`
- [RCDragManagerProd\Repositories\DriverRepository.cs:306] ComputeEventsWonFromSavedSessions : `SELECT SessionData FROM RaceSessions`

## RCDragManagerProd.Repositories.RaceSessionRepository

- File: `RCDragManagerProd\Repositories\RaceSessionRepository.cs`
- SQL usages: 4

- [RCDragManagerProd\Repositories\RaceSessionRepository.cs:133] LoadSession : `SELECT SessionData FROM RaceSessions WHERE Id = @Id`
- [RCDragManagerProd\Repositories\RaceSessionRepository.cs:165] DeleteSession : `[DB][SessionRepo] DeleteSession(id={id})`
- [RCDragManagerProd\Repositories\RaceSessionRepository.cs:167] DeleteSession : `DELETE FROM RaceSessions WHERE Id = @Id`
- [RCDragManagerProd\Repositories\RaceSessionRepository.cs:172] DeleteSession : `[DB][SessionRepo] DeleteSession → OK`

