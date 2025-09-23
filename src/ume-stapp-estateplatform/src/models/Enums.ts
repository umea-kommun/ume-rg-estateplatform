/**
 * Size for app content. Default is used to show a public form.
 * Wide is used to provide more space for content, for example in admin.
 */
export enum AppContentSize {
	Narrow = 'Size-Narrow',
	Default = 'Size-Default',
	Wide = 'Size-Wide',
}

export enum AppHeaderTitle {
	Default = 'default',
	Internal = 'internal',
	InternalConsent = 'internalConsent',
	AdminConsent = 'adminConsent',
	AgentConsent = 'agentConsent',
	InternalKvittens = 'internalKvittens',
	AgentKvittens = 'agentKvittens',
	InternalDefaultPasswords = 'internalDefaultPasswords',
}

/** ErrorCode som kommer från backend */
export enum ErrorCode {}

/** Mutationtypes for store */
export enum MutationType {
	HideWarningMessage = 'hideWarningMessage',

	// User
	UserLogIn = 'userLogIn',
	UserLogOut = 'userLogOut',
	UserEnterPage = 'userEnterPage',

	// Guardian consent
	GetGuardianConsent = 'getGuardianConsent',
	GetGuardianConsentList = 'getGuardianConsentList',
	UpdateGuardianConsentListAnswer = 'updateGuardianConsentListAnswer',
	GetChildren = 'getChildren',

	// Consent template admin
	GetConsentTemplates = 'getConsentTemplates',
	GetConsentTemplate = 'getConsentTemplate',
	NewConsentTemplate = 'newConsentTemplate',
	UpdateConsentTemplate = 'updateConsentTemplate',
	GetConsentTemplateUnitTypes = 'getConsentTemplateUnitTypes',
	GetConsentTemplateGroups = 'getConsentTemplateGroups',

	// Consent consumer
	GetConsumerList = 'getConsumerList',
	GetConsumerDetails = 'getConsumerDetails',

	// Consent Agent
	UpdateConsentAgentConsentList = 'updateConsentAgentConsentList',
	UpdateConsentAgentConsentStatus = 'updateConsentAgentConsentStatus',

	// Tester
	GetTesterSchoolUnits = 'getTesterSchoolUnits',
	SetTesterTestAs = 'setTesterTestAs',

	// Error handler
	SetError = 'setError',

	// Kvittens
	UpdateKvittensList = 'updateKvittensList',
	UpdateAnswerInKvittensList = 'updateAnswerInKvittensList',
	UpdateKvittensAgentList = 'updateKvittensAgentList',
	UpdateAnswerInAgentKvittensList = 'updateAnswerInAgentKvittensList',
}

export enum DispatchType {
	GetChildren = 'getChildren',
	GetConsentTemplates = 'getConsentTemplates',
	GetConsentTemplate = 'getConsentTemplate',
	SaveConsentTemplate = 'saveConsentTemplate',
	GetConsentTemplateUnitTypes = 'getConsentTemplateUnitTypes',
	GetConsentTemplateUnits = 'getConsentTemplateUnits',
	GetConsentTemplateUnitGroups = 'getConsentTemplateUnitGroups',
	GetConsentList = 'getConsentList',

	GetConsentConsumerList = 'getConsentConsumerList',
	GetConsentConsumerTemplateWithConsents = 'getConsentConsumerTemplateWithConsents',

	GetTesterSchoolUnits = 'getTesterSchoolUnits',
	GetTesterSchoolTeachers = 'getTesterSchoolTeachers',

	GetStudentsInGroup = 'getStudentsInGroup',

	// Kvittens
	GetKvittensList = 'getKvittensList',
	GetKvittensDetails = 'getKvittensDetails',
	SaveKvittensAnswer = 'saveKvittensAnswer',
	GetKvittensFilterGroups = 'getKvittensFilterGroups',
	GetKvittensSummary = 'getKvittensSummary',
	GetAgentKvittensList = 'getAgentKvittensList',
	GetAgentKvittensDetails = 'getAgentKvittensDetails',
	AgentAnswerKvittens = 'agentAnswerKvittens',

	// Password
	GetConsumerGroupsAndSchools = 'getConsumerGroupsAndSchools',
	GetDefaultPasswordAssignments = 'getDefaultPasswordAssignments',

	// Grades
	GetGrades = 'getGrades',
	DownloadGrade = 'downloadGrade',
}

export enum ConsentStatus {
	Approved = 1,
	Denied = 2,
	Pending = 3,
	New = 4,
}
export enum UserConsentStatus {
	NotAnswered,
	Approved,
	Rejected,
	NotApplicable,
}

export enum ConsentTemplateStatus {
	Draft = 0,
	Published = 1,
}

export enum ConsentTemplateGuid {
	New = 'skapa-ny',
}

export enum TemplateConnectionType {
	Unit = 'Unit',
	Class = 'Class',
	EducationGroup = 'EducationGroup',
	Skolform = 'Skolform',
	Department = 'Department',
}
