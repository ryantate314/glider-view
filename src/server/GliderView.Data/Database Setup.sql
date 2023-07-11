USE [master]
GO
/****** Object:  Database [GliderView]    Script Date: 7/11/2023 07:31:50 ******/
CREATE DATABASE [GliderView]
 CONTAINMENT = NONE
 ON  PRIMARY 
( NAME = N'GliderView', FILENAME = N'/var/opt/mssql/data/GliderView.mdf' , SIZE = 73728KB , MAXSIZE = UNLIMITED, FILEGROWTH = 65536KB )
 LOG ON 
( NAME = N'GliderView_log', FILENAME = N'/var/opt/mssql/data/GliderView_log.ldf' , SIZE = 401408KB , MAXSIZE = 2048GB , FILEGROWTH = 65536KB )
 WITH CATALOG_COLLATION = DATABASE_DEFAULT
GO
ALTER DATABASE [GliderView] SET COMPATIBILITY_LEVEL = 140
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [GliderView].[dbo].[sp_fulltext_database] @action = 'enable'
end
GO
ALTER DATABASE [GliderView] SET ANSI_NULL_DEFAULT OFF 
GO
ALTER DATABASE [GliderView] SET ANSI_NULLS OFF 
GO
ALTER DATABASE [GliderView] SET ANSI_PADDING OFF 
GO
ALTER DATABASE [GliderView] SET ANSI_WARNINGS OFF 
GO
ALTER DATABASE [GliderView] SET ARITHABORT OFF 
GO
ALTER DATABASE [GliderView] SET AUTO_CLOSE OFF 
GO
ALTER DATABASE [GliderView] SET AUTO_SHRINK OFF 
GO
ALTER DATABASE [GliderView] SET AUTO_UPDATE_STATISTICS ON 
GO
ALTER DATABASE [GliderView] SET CURSOR_CLOSE_ON_COMMIT OFF 
GO
ALTER DATABASE [GliderView] SET CURSOR_DEFAULT  GLOBAL 
GO
ALTER DATABASE [GliderView] SET CONCAT_NULL_YIELDS_NULL OFF 
GO
ALTER DATABASE [GliderView] SET NUMERIC_ROUNDABORT OFF 
GO
ALTER DATABASE [GliderView] SET QUOTED_IDENTIFIER OFF 
GO
ALTER DATABASE [GliderView] SET RECURSIVE_TRIGGERS OFF 
GO
ALTER DATABASE [GliderView] SET  DISABLE_BROKER 
GO
ALTER DATABASE [GliderView] SET AUTO_UPDATE_STATISTICS_ASYNC OFF 
GO
ALTER DATABASE [GliderView] SET DATE_CORRELATION_OPTIMIZATION OFF 
GO
ALTER DATABASE [GliderView] SET TRUSTWORTHY OFF 
GO
ALTER DATABASE [GliderView] SET ALLOW_SNAPSHOT_ISOLATION OFF 
GO
ALTER DATABASE [GliderView] SET PARAMETERIZATION SIMPLE 
GO
ALTER DATABASE [GliderView] SET READ_COMMITTED_SNAPSHOT OFF 
GO
ALTER DATABASE [GliderView] SET HONOR_BROKER_PRIORITY OFF 
GO
ALTER DATABASE [GliderView] SET RECOVERY FULL 
GO
ALTER DATABASE [GliderView] SET  MULTI_USER 
GO
ALTER DATABASE [GliderView] SET PAGE_VERIFY CHECKSUM  
GO
ALTER DATABASE [GliderView] SET DB_CHAINING OFF 
GO
ALTER DATABASE [GliderView] SET FILESTREAM( NON_TRANSACTED_ACCESS = OFF ) 
GO
ALTER DATABASE [GliderView] SET TARGET_RECOVERY_TIME = 60 SECONDS 
GO
ALTER DATABASE [GliderView] SET DELAYED_DURABILITY = DISABLED 
GO
ALTER DATABASE [GliderView] SET ACCELERATED_DATABASE_RECOVERY = OFF  
GO
ALTER DATABASE [GliderView] SET QUERY_STORE = OFF
GO
USE [GliderView]
GO
/****** Object:  User [gliderViewer-admin]    Script Date: 7/11/2023 07:31:51 ******/
CREATE USER [gliderViewer-admin] FOR LOGIN [gliderViewer-admin] WITH DEFAULT_SCHEMA=[dbo]
GO
/****** Object:  User [gliderViewer]    Script Date: 7/11/2023 07:31:51 ******/
CREATE USER [gliderViewer] FOR LOGIN [gliderViewer] WITH DEFAULT_SCHEMA=[dbo]
GO
ALTER ROLE [db_owner] ADD MEMBER [gliderViewer-admin]
GO
/****** Object:  Schema [HangFire]    Script Date: 7/11/2023 07:31:51 ******/
CREATE SCHEMA [HangFire]
GO
/****** Object:  UserDefinedTableType [dbo].[IdList]    Script Date: 7/11/2023 07:31:51 ******/
CREATE TYPE [dbo].[IdList] AS TABLE(
	[Id] [uniqueidentifier] NOT NULL
)
GO
/****** Object:  UserDefinedTableType [dbo].[Statistic]    Script Date: 7/11/2023 07:31:51 ******/
CREATE TYPE [dbo].[Statistic] AS TABLE(
	[StatisticId] [int] NOT NULL,
	[Value] [float] NULL
)
GO
/****** Object:  UserDefinedTableType [dbo].[Waypoint]    Script Date: 7/11/2023 07:31:51 ******/
CREATE TYPE [dbo].[Waypoint] AS TABLE(
	[WaypointId] [int] NULL,
	[Latitude] [decimal](6, 4) NOT NULL,
	[Longitude] [decimal](7, 4) NOT NULL,
	[GpsAltitudeMeters] [smallint] NULL,
	[Date] [datetime] NOT NULL,
	[FlightEvent] [tinyint] NULL
)
GO
/****** Object:  Table [dbo].[Aircraft]    Script Date: 7/11/2023 07:31:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Aircraft](
	[AircraftId] [int] IDENTITY(1,1) NOT NULL,
	[AircraftGuid] [uniqueidentifier] NOT NULL,
	[TrackerId] [char](6) NULL,
	[Description] [varchar](32) NULL,
	[Registration] [char](6) NULL,
	[NumSeats] [tinyint] NULL,
	[IsGlider] [bit] NULL,
	[AddedDate] [datetime] NOT NULL,
	[IsDeleted] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[AircraftId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[AircraftRates]    Script Date: 7/11/2023 07:31:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AircraftRates](
	[AircraftRateId] [int] IDENTITY(1,1) NOT NULL,
	[AircraftId] [int] NOT NULL,
	[EffectiveDate] [datetime] NOT NULL,
	[ExpirationDate] [datetime] NULL,
	[RentalCostPerHour] [smallmoney] NOT NULL,
	[MinRentalHours] [float] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[AircraftRateId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Airfield]    Script Date: 7/11/2023 07:31:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Airfield](
	[AirfieldId] [int] NOT NULL,
	[FaaId] [varchar](5) NOT NULL,
	[ElevationMeters] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[AirfieldId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_Airfield_FaaId] UNIQUE NONCLUSTERED 
(
	[FaaId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Flight]    Script Date: 7/11/2023 07:31:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Flight](
	[FlightId] [int] IDENTITY(1,1) NOT NULL,
	[FlightGuid] [uniqueidentifier] NOT NULL,
	[StartDate] [datetime] NOT NULL,
	[EndDate] [datetime] NULL,
	[AircraftId] [int] NULL,
	[TowId] [int] NULL,
	[IgcFilename] [varchar](255) NULL,
	[AddedDate] [datetime] NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[ModifiedDate] [datetime] NULL,
	[ContestId] [varchar](3) NULL,
	[AirfieldId] [int] NULL,
PRIMARY KEY CLUSTERED 
(
	[FlightId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[FlightEventType]    Script Date: 7/11/2023 07:31:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FlightEventType](
	[FlightEventTypeId] [tinyint] NOT NULL,
	[Description] [varchar](32) NULL,
PRIMARY KEY CLUSTERED 
(
	[FlightEventTypeId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[FlightStatistics]    Script Date: 7/11/2023 07:31:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FlightStatistics](
	[FLightStatisticId] [int] IDENTITY(1,1) NOT NULL,
	[FlightId] [int] NOT NULL,
	[StatisticId] [int] NOT NULL,
	[Value] [float] NULL,
	[DateAdded] [datetime] NOT NULL,
	[IsDeleted] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[FLightStatisticId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[FlightStatisticsOld]    Script Date: 7/11/2023 07:31:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[FlightStatisticsOld](
	[FlightStatisticsId] [int] IDENTITY(1,1) NOT NULL,
	[FlightId] [int] NOT NULL,
	[ReleaseHeight] [int] NULL,
	[AltitudeGained] [int] NULL,
	[DistanceTraveled] [float] NULL,
	[MaxAltitude] [int] NULL,
	[PatternEntryAltitude] [int] NULL,
	[DateAdded] [datetime] NOT NULL,
	[IsDeleted] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[FlightStatisticsId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Invitation]    Script Date: 7/11/2023 07:31:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Invitation](
	[InvitationId] [int] IDENTITY(1,1) NOT NULL,
	[UserId] [int] NOT NULL,
	[Token] [char](32) NOT NULL,
	[IssuedDate] [datetime] NOT NULL,
	[ExpirationDate] [datetime] NOT NULL,
	[IsDeleted] [bit] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[InvitationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Occupant]    Script Date: 7/11/2023 07:31:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Occupant](
	[OccupantId] [int] IDENTITY(1,1) NOT NULL,
	[FlightId] [int] NOT NULL,
	[UserId] [int] NULL,
	[FlightNumber] [int] NULL,
	[Notes] [varchar](max) NULL,
	[Name] [varchar](100) NULL,
PRIMARY KEY CLUSTERED 
(
	[OccupantId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Rates]    Script Date: 7/11/2023 07:31:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Rates](
	[RateId] [int] IDENTITY(1,1) NOT NULL,
	[EffectiveDate] [datetime] NOT NULL,
	[ExpirationDate] [datetime] NULL,
	[HookupCost] [smallmoney] NOT NULL,
	[CostPerHundredFeet] [smallmoney] NOT NULL,
	[MinTowHeight] [int] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[RateId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Statistic]    Script Date: 7/11/2023 07:31:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Statistic](
	[StatisticId] [int] NOT NULL,
	[Description] [varchar](32) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[StatisticId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[User]    Script Date: 7/11/2023 07:31:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[User](
	[UserId] [int] IDENTITY(1,1) NOT NULL,
	[UserGuid] [uniqueidentifier] NOT NULL,
	[IdentityId] [varchar](100) NULL,
	[Role] [char](1) NOT NULL,
	[Email] [varchar](255) NOT NULL,
	[Name] [varchar](255) NOT NULL,
	[HashedPassword] [varchar](255) NULL,
	[FailedLoginAttempts] [tinyint] NOT NULL,
	[IsLocked] [bit] NOT NULL,
	[AddedDate] [datetime] NOT NULL,
	[IsDeleted] [bit] NOT NULL,
	[ModifiedDate] [datetime] NULL,
PRIMARY KEY CLUSTERED 
(
	[UserId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[UserRole]    Script Date: 7/11/2023 07:31:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[UserRole](
	[RoleId] [int] IDENTITY(1,1) NOT NULL,
	[RoleCode] [char](1) NOT NULL,
	[Description] [varchar](100) NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[RoleId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED 
(
	[RoleCode] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Waypoint]    Script Date: 7/11/2023 07:31:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Waypoint](
	[WaypointId] [int] IDENTITY(1,1) NOT NULL,
	[FlightId] [int] NOT NULL,
	[Latitude] [decimal](6, 4) NOT NULL,
	[Longitude] [decimal](7, 4) NOT NULL,
	[GpsAltitudeMeters] [smallint] NULL,
	[Date] [datetime] NOT NULL,
	[FlightEvent] [tinyint] NULL,
PRIMARY KEY CLUSTERED 
(
	[WaypointId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [HangFire].[AggregatedCounter]    Script Date: 7/11/2023 07:31:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [HangFire].[AggregatedCounter](
	[Key] [nvarchar](100) NOT NULL,
	[Value] [bigint] NOT NULL,
	[ExpireAt] [datetime] NULL,
 CONSTRAINT [PK_HangFire_CounterAggregated] PRIMARY KEY CLUSTERED 
(
	[Key] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [HangFire].[Counter]    Script Date: 7/11/2023 07:31:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [HangFire].[Counter](
	[Key] [nvarchar](100) NOT NULL,
	[Value] [int] NOT NULL,
	[ExpireAt] [datetime] NULL
) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [CX_HangFire_Counter]    Script Date: 7/11/2023 07:31:51 ******/
CREATE CLUSTERED INDEX [CX_HangFire_Counter] ON [HangFire].[Counter]
(
	[Key] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Table [HangFire].[Hash]    Script Date: 7/11/2023 07:31:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [HangFire].[Hash](
	[Key] [nvarchar](100) NOT NULL,
	[Field] [nvarchar](100) NOT NULL,
	[Value] [nvarchar](max) NULL,
	[ExpireAt] [datetime2](7) NULL,
 CONSTRAINT [PK_HangFire_Hash] PRIMARY KEY CLUSTERED 
(
	[Key] ASC,
	[Field] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [HangFire].[Job]    Script Date: 7/11/2023 07:31:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [HangFire].[Job](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[StateId] [bigint] NULL,
	[StateName] [nvarchar](20) NULL,
	[InvocationData] [nvarchar](max) NOT NULL,
	[Arguments] [nvarchar](max) NOT NULL,
	[CreatedAt] [datetime] NOT NULL,
	[ExpireAt] [datetime] NULL,
 CONSTRAINT [PK_HangFire_Job] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [HangFire].[JobParameter]    Script Date: 7/11/2023 07:31:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [HangFire].[JobParameter](
	[JobId] [bigint] NOT NULL,
	[Name] [nvarchar](40) NOT NULL,
	[Value] [nvarchar](max) NULL,
 CONSTRAINT [PK_HangFire_JobParameter] PRIMARY KEY CLUSTERED 
(
	[JobId] ASC,
	[Name] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [HangFire].[JobQueue]    Script Date: 7/11/2023 07:31:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [HangFire].[JobQueue](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[JobId] [bigint] NOT NULL,
	[Queue] [nvarchar](50) NOT NULL,
	[FetchedAt] [datetime] NULL,
 CONSTRAINT [PK_HangFire_JobQueue] PRIMARY KEY CLUSTERED 
(
	[Queue] ASC,
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [HangFire].[List]    Script Date: 7/11/2023 07:31:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [HangFire].[List](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[Key] [nvarchar](100) NOT NULL,
	[Value] [nvarchar](max) NULL,
	[ExpireAt] [datetime] NULL,
 CONSTRAINT [PK_HangFire_List] PRIMARY KEY CLUSTERED 
(
	[Key] ASC,
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [HangFire].[Schema]    Script Date: 7/11/2023 07:31:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [HangFire].[Schema](
	[Version] [int] NOT NULL,
 CONSTRAINT [PK_HangFire_Schema] PRIMARY KEY CLUSTERED 
(
	[Version] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [HangFire].[Server]    Script Date: 7/11/2023 07:31:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [HangFire].[Server](
	[Id] [nvarchar](200) NOT NULL,
	[Data] [nvarchar](max) NULL,
	[LastHeartbeat] [datetime] NOT NULL,
 CONSTRAINT [PK_HangFire_Server] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [HangFire].[Set]    Script Date: 7/11/2023 07:31:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [HangFire].[Set](
	[Key] [nvarchar](100) NOT NULL,
	[Score] [float] NOT NULL,
	[Value] [nvarchar](256) NOT NULL,
	[ExpireAt] [datetime] NULL,
 CONSTRAINT [PK_HangFire_Set] PRIMARY KEY CLUSTERED 
(
	[Key] ASC,
	[Value] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [HangFire].[State]    Script Date: 7/11/2023 07:31:51 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [HangFire].[State](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[JobId] [bigint] NOT NULL,
	[Name] [nvarchar](20) NOT NULL,
	[Reason] [nvarchar](100) NULL,
	[CreatedAt] [datetime] NOT NULL,
	[Data] [nvarchar](max) NULL,
 CONSTRAINT [PK_HangFire_State] PRIMARY KEY CLUSTERED 
(
	[JobId] ASC,
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Index [IX_HangFire_AggregatedCounter_ExpireAt]    Script Date: 7/11/2023 07:31:51 ******/
CREATE NONCLUSTERED INDEX [IX_HangFire_AggregatedCounter_ExpireAt] ON [HangFire].[AggregatedCounter]
(
	[ExpireAt] ASC
)
WHERE ([ExpireAt] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_HangFire_Hash_ExpireAt]    Script Date: 7/11/2023 07:31:51 ******/
CREATE NONCLUSTERED INDEX [IX_HangFire_Hash_ExpireAt] ON [HangFire].[Hash]
(
	[ExpireAt] ASC
)
WHERE ([ExpireAt] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_HangFire_Job_ExpireAt]    Script Date: 7/11/2023 07:31:51 ******/
CREATE NONCLUSTERED INDEX [IX_HangFire_Job_ExpireAt] ON [HangFire].[Job]
(
	[ExpireAt] ASC
)
INCLUDE([StateName]) 
WHERE ([ExpireAt] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_HangFire_Job_StateName]    Script Date: 7/11/2023 07:31:51 ******/
CREATE NONCLUSTERED INDEX [IX_HangFire_Job_StateName] ON [HangFire].[Job]
(
	[StateName] ASC
)
WHERE ([StateName] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_HangFire_List_ExpireAt]    Script Date: 7/11/2023 07:31:51 ******/
CREATE NONCLUSTERED INDEX [IX_HangFire_List_ExpireAt] ON [HangFire].[List]
(
	[ExpireAt] ASC
)
WHERE ([ExpireAt] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_HangFire_Server_LastHeartbeat]    Script Date: 7/11/2023 07:31:51 ******/
CREATE NONCLUSTERED INDEX [IX_HangFire_Server_LastHeartbeat] ON [HangFire].[Server]
(
	[LastHeartbeat] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
/****** Object:  Index [IX_HangFire_Set_ExpireAt]    Script Date: 7/11/2023 07:31:51 ******/
CREATE NONCLUSTERED INDEX [IX_HangFire_Set_ExpireAt] ON [HangFire].[Set]
(
	[ExpireAt] ASC
)
WHERE ([ExpireAt] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
/****** Object:  Index [IX_HangFire_Set_Score]    Script Date: 7/11/2023 07:31:51 ******/
CREATE NONCLUSTERED INDEX [IX_HangFire_Set_Score] ON [HangFire].[Set]
(
	[Key] ASC,
	[Score] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Aircraft] ADD  DEFAULT (newid()) FOR [AircraftGuid]
GO
ALTER TABLE [dbo].[Aircraft] ADD  DEFAULT (getdate()) FOR [AddedDate]
GO
ALTER TABLE [dbo].[Aircraft] ADD  DEFAULT ((0)) FOR [IsDeleted]
GO
ALTER TABLE [dbo].[Flight] ADD  DEFAULT (newid()) FOR [FlightGuid]
GO
ALTER TABLE [dbo].[Flight] ADD  DEFAULT (getdate()) FOR [AddedDate]
GO
ALTER TABLE [dbo].[Flight] ADD  DEFAULT ((0)) FOR [IsDeleted]
GO
ALTER TABLE [dbo].[FlightStatistics] ADD  CONSTRAINT [DF_FlightStatistics_DateAdded]  DEFAULT (getdate()) FOR [DateAdded]
GO
ALTER TABLE [dbo].[FlightStatistics] ADD  CONSTRAINT [DF_FlightStatistics_IsDeleted]  DEFAULT ((0)) FOR [IsDeleted]
GO
ALTER TABLE [dbo].[FlightStatisticsOld] ADD  DEFAULT (getdate()) FOR [DateAdded]
GO
ALTER TABLE [dbo].[FlightStatisticsOld] ADD  DEFAULT ((0)) FOR [IsDeleted]
GO
ALTER TABLE [dbo].[Invitation] ADD  DEFAULT (getdate()) FOR [IssuedDate]
GO
ALTER TABLE [dbo].[Invitation] ADD  DEFAULT ((0)) FOR [IsDeleted]
GO
ALTER TABLE [dbo].[User] ADD  DEFAULT (newid()) FOR [UserGuid]
GO
ALTER TABLE [dbo].[User] ADD  DEFAULT ((0)) FOR [FailedLoginAttempts]
GO
ALTER TABLE [dbo].[User] ADD  DEFAULT ((0)) FOR [IsLocked]
GO
ALTER TABLE [dbo].[User] ADD  DEFAULT (getdate()) FOR [AddedDate]
GO
ALTER TABLE [dbo].[User] ADD  DEFAULT ((0)) FOR [IsDeleted]
GO
ALTER TABLE [dbo].[AircraftRates]  WITH CHECK ADD  CONSTRAINT [FK_AircraftRates_AircraftId_Aircraft] FOREIGN KEY([AircraftId])
REFERENCES [dbo].[Aircraft] ([AircraftId])
GO
ALTER TABLE [dbo].[AircraftRates] CHECK CONSTRAINT [FK_AircraftRates_AircraftId_Aircraft]
GO
ALTER TABLE [dbo].[Flight]  WITH CHECK ADD FOREIGN KEY([AircraftId])
REFERENCES [dbo].[Aircraft] ([AircraftId])
GO
ALTER TABLE [dbo].[Flight]  WITH CHECK ADD FOREIGN KEY([TowId])
REFERENCES [dbo].[Flight] ([FlightId])
GO
ALTER TABLE [dbo].[Flight]  WITH CHECK ADD  CONSTRAINT [FK_Flight_AirfieldId_Airfield] FOREIGN KEY([AirfieldId])
REFERENCES [dbo].[Airfield] ([AirfieldId])
GO
ALTER TABLE [dbo].[Flight] CHECK CONSTRAINT [FK_Flight_AirfieldId_Airfield]
GO
ALTER TABLE [dbo].[FlightStatistics]  WITH CHECK ADD  CONSTRAINT [FK_FlightStatistics_FlightId_Flight] FOREIGN KEY([FlightId])
REFERENCES [dbo].[Flight] ([FlightId])
GO
ALTER TABLE [dbo].[FlightStatistics] CHECK CONSTRAINT [FK_FlightStatistics_FlightId_Flight]
GO
ALTER TABLE [dbo].[FlightStatistics]  WITH CHECK ADD  CONSTRAINT [FK_FlightStatistics_StatisticId_Statistic] FOREIGN KEY([StatisticId])
REFERENCES [dbo].[Statistic] ([StatisticId])
GO
ALTER TABLE [dbo].[FlightStatistics] CHECK CONSTRAINT [FK_FlightStatistics_StatisticId_Statistic]
GO
ALTER TABLE [dbo].[FlightStatisticsOld]  WITH CHECK ADD FOREIGN KEY([FlightId])
REFERENCES [dbo].[Flight] ([FlightId])
GO
ALTER TABLE [dbo].[Invitation]  WITH CHECK ADD FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([UserId])
GO
ALTER TABLE [dbo].[Occupant]  WITH CHECK ADD FOREIGN KEY([FlightId])
REFERENCES [dbo].[Flight] ([FlightId])
GO
ALTER TABLE [dbo].[Occupant]  WITH CHECK ADD FOREIGN KEY([UserId])
REFERENCES [dbo].[User] ([UserId])
GO
ALTER TABLE [dbo].[User]  WITH CHECK ADD FOREIGN KEY([Role])
REFERENCES [dbo].[UserRole] ([RoleCode])
GO
ALTER TABLE [dbo].[Waypoint]  WITH CHECK ADD FOREIGN KEY([FlightId])
REFERENCES [dbo].[Flight] ([FlightId])
GO
ALTER TABLE [dbo].[Waypoint]  WITH CHECK ADD FOREIGN KEY([FlightEvent])
REFERENCES [dbo].[FlightEventType] ([FlightEventTypeId])
GO
ALTER TABLE [HangFire].[JobParameter]  WITH CHECK ADD  CONSTRAINT [FK_HangFire_JobParameter_Job] FOREIGN KEY([JobId])
REFERENCES [HangFire].[Job] ([Id])
ON UPDATE CASCADE
ON DELETE CASCADE
GO
ALTER TABLE [HangFire].[JobParameter] CHECK CONSTRAINT [FK_HangFire_JobParameter_Job]
GO
ALTER TABLE [HangFire].[State]  WITH CHECK ADD  CONSTRAINT [FK_HangFire_State_Job] FOREIGN KEY([JobId])
REFERENCES [HangFire].[Job] ([Id])
ON UPDATE CASCADE
ON DELETE CASCADE
GO
ALTER TABLE [HangFire].[State] CHECK CONSTRAINT [FK_HangFire_State_Job]
GO
USE [master]
GO
ALTER DATABASE [GliderView] SET  READ_WRITE 
GO
