SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[NexusUsers](
	[UserId] [int] IDENTITY(1,1) NOT NULL,
	[Email] [varchar](320) NOT NULL,
	[AlternateEmail] [varchar](320) NULL,
	[Password] [varchar](100) NULL,
	[Phone] [varchar](15) NULL,
	[PhoneProviderId] [int] NOT NULL,
	[FirstName] [varchar](100) NULL,
	[LastName] [varchar](100) NULL,
	[NotifyByEmail] [bit] NOT NULL,
	[NotifyBySms] [bit] NOT NULL,
	[IsActive] [bit] NOT NULL,
	[EmailConfirmed] [bit] NOT NULL,
	[AlternateEmailConfirmed] [bit] NOT NULL,
	[PhoneConfirmed] [bit] NOT NULL
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[NexusUsers] ADD  CONSTRAINT [PK_NexusUsers] PRIMARY KEY CLUSTERED 
(
	[UserId] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON
GO
CREATE UNIQUE NONCLUSTERED INDEX [Index_NexusUsers_Email] ON [dbo].[NexusUsers]
(
	[Email] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[NexusUsers] ADD  CONSTRAINT [DEFAULT_NexusUsers_NotifyByEmail]  DEFAULT ((0)) FOR [NotifyByEmail]
GO
ALTER TABLE [dbo].[NexusUsers] ADD  CONSTRAINT [DEFAULT_NexusUsers_NotifyBySms]  DEFAULT ((0)) FOR [NotifyBySms]
GO
ALTER TABLE [dbo].[NexusUsers] ADD  CONSTRAINT [DEFAULT_NexusUsers_IsActive]  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[NexusUsers] ADD  CONSTRAINT [DEFAULT_NexusUsers_EmailConfirmed]  DEFAULT ((0)) FOR [EmailConfirmed]
GO
ALTER TABLE [dbo].[NexusUsers] ADD  CONSTRAINT [DEFAULT_NexusUsers_AlternateEmailConfirmed]  DEFAULT ((0)) FOR [AlternateEmailConfirmed]
GO
ALTER TABLE [dbo].[NexusUsers] ADD  CONSTRAINT [DEFAULT_NexusUsers_PhoneConfirmed]  DEFAULT ((0)) FOR [PhoneConfirmed]
GO
