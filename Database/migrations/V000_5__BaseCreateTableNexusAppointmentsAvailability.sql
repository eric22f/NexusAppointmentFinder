SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[NexusAppointmentsAvailability](
	[AppointmentId] [int] IDENTITY(1,1) NOT NULL,
	[AppointmentDate] [datetime] NOT NULL,
	[LocationId] [int] NOT NULL,
	[Openings] [smallint] NOT NULL,
	[TotalSlots] [smallint] NOT NULL,
	[Pending] [smallint] NOT NULL,
	[Conflicts] [smallint] NOT NULL,
	[Duration] [smallint] NOT NULL,
	[CreatedDate] [datetime] NOT NULL
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[NexusAppointmentsAvailability] ADD  CONSTRAINT [PK_NexusAppointmentsAvailability] PRIMARY KEY CLUSTERED 
(
	[AppointmentId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE UNIQUE NONCLUSTERED INDEX [Index_NexusAppointmentsByDate_AppointmentDate_LocationID_Unique] ON [dbo].[NexusAppointmentsAvailability]
(
	[AppointmentDate] ASC,
	[LocationId] ASC
)
INCLUDE([Openings],[TotalSlots],[Pending],[Conflicts],[Duration]) WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[NexusAppointmentsAvailability] ADD  CONSTRAINT [DEFAULT_NexusAppointmentsAvailability_Openings]  DEFAULT ((0)) FOR [Openings]
GO
ALTER TABLE [dbo].[NexusAppointmentsAvailability] ADD  CONSTRAINT [DEFAULT_NexusAppointmentsAvailability_TotalSlots]  DEFAULT ((0)) FOR [TotalSlots]
GO
ALTER TABLE [dbo].[NexusAppointmentsAvailability] ADD  CONSTRAINT [DEFAULT_NexusAppointmentsAvailability_Pending]  DEFAULT ((0)) FOR [Pending]
GO
ALTER TABLE [dbo].[NexusAppointmentsAvailability] ADD  CONSTRAINT [DEFAULT_NexusAppointmentsAvailability_Conflicts]  DEFAULT ((0)) FOR [Conflicts]
GO
ALTER TABLE [dbo].[NexusAppointmentsAvailability] ADD  CONSTRAINT [DEFAULT_NexusAppointmentsAvailability_Duration]  DEFAULT ((0)) FOR [Duration]
GO
ALTER TABLE [dbo].[NexusAppointmentsAvailability] ADD  CONSTRAINT [DEFAULT_NexusAppointmentsAvailability_CreatedDate]  DEFAULT (getdate()) FOR [CreatedDate]
GO
