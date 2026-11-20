// Every word of the builder's help: what each page is for, how the builder works, the glossary, the tour and the
// welcome. Static on purpose: help works with no model and no network.

export type PageId = 'scenarios' | 'context' | 'questions' | 'summary' | 'log' | 'topology' | 'grayspace' | 'updraft' | 'finish' | 'admin';

export interface PageHelp {
  name: string;
  /** What the page is for, in one sentence. */
  lead: string;
  /** What to do here, in order. */
  steps: string[];
  /** What "done" looks like. */
  done?: string;
  notes?: string[];
  /** The page to go to next, in the same scenario. */
  next?: PageId;
}

export interface Concept {
  id: string;
  title: string;
  body: string[];
  /** A numbered list after the body, each point a bold lead and its explanation. */
  points?: { lead: string; body: string }[];
}

export interface Term {
  id: string;
  term: string;
  definition: string;
  /** Where in the builder you meet it. */
  where: string;
  /** Words that mark the term where it appears in a stage title or question. Tool words that appear everywhere have none. */
  match?: string[];
}

export interface TourStop {
  /** The `data-tour` value of the element the stop points at. */
  target: string;
  title: string;
  body: string;
}

export const PAGES: Record<PageId, PageHelp> = {
  scenarios: {
    name: 'Scenarios',
    lead: 'Create a scenario or reopen one from the list.',
    steps: [
      'For a new scenario, enter a name and choose a scenario type.',
      'Select Create to open the Context page.',
      'To resume a scenario, select its name. The Stage column shows your progress.'
    ],
    done: 'New scenarios open on the Context page. Existing scenarios open on Questions.',
    notes: ['You can update a scenario to a newer version of its type. You can’t switch it to a different type.']
  },
  context: {
    name: 'Context',
    lead: 'Add background material for the agent to use in its recommendations.',
    steps: [
      'Paste notes, add a link or upload a document, such as an agenda or report.',
      'Check the Sources list and remove anything you don’t want the agent to use.',
      'Select Start the questions. You can add sources later.'
    ],
    done: 'You can start the questions with or without sources.',
    notes: ['The builder keeps the text of each source. When the agent uses a source, it names it in the recommendation.'],
    next: 'questions'
  },
  questions: {
    name: 'Questions',
    lead: 'Each row records one decision about the scenario.',
    steps: [
      'In the open stage, select Answer on a row or start with the agent.',
      'Enter your answer, accept a recommendation or select You decide on a question card to let the agent answer it.',
      'Answer every question and save any required draft. Then select Open the next stage.'
    ],
    done: 'The last stage shows Complete.',
    notes: ['You can change answers in the current stage or any closed stage. The Decision Log keeps the previous answers.'],
    next: 'summary'
  },
  summary: {
    name: 'Executive Summary',
    lead: 'Prepare a short description of the scenario for the exercise sponsor.',
    steps: [
      'Write the summary yourself or ask the agent to draft it from your answers.',
      'Review the text, make any changes and select Save.'
    ],
    done: 'The summary is saved and meets the draft requirement for its stage.',
    next: 'log'
  },
  log: {
    name: 'Decision Log',
    lead: 'Review answers, drafts and proposals, newest first.',
    steps: [
      'Find the decision you want to review. Each row shows who made the change, the previous and new values, and any reason given.',
      'To revise an answer, open it on the Questions page.'
    ],
    next: 'topology'
  },
  topology: {
    name: 'Topology',
    lead: 'Use the network answers to plan a range deployment.',
    steps: [
      'Answer the network questions for the scenario.',
      'Open GraySpace to configure the range or Updraft to preview its lab settings.',
      'For GraySpace, validate the enclave before you deploy it.'
    ],
    notes: ['Each scenario can have one deployment target.'],
    next: 'finish'
  },
  grayspace: {
    name: 'Deploy to GraySpace',
    lead: 'Configure and deploy a GraySpace enclave for this scenario.',
    steps: [
      'An admin connects GraySpace using its deployment URL and API token.',
      'Select Deploy everything to use the current settings, or review the settings individually.',
      'For a manual deployment, validate and save the enclave, review the plan, then create an instance.'
    ],
    done: 'An instance is running and its job has finished.',
    notes: ['Settings based on your answers list those answers under From Your Answers.'],
    next: 'finish'
  },
  updraft: {
    name: 'Deploy to Updraft',
    lead: 'Preview the settings for an Updraft lab.',
    steps: [
      'Compare the lab settings with your scenario answers. The preview is read-only.'
    ],
    next: 'finish'
  },
  finish: {
    name: 'Finish',
    lead: 'Export your scenario or publish it to Chronicle at any stage.',
    steps: [
      'Download JSON for another system or Markdown for a readable copy of the answers.',
      'Choose a document template for your audience, then preview or download it.',
      'Select Publish to Chronicle to add the scenario’s World and Intent to the event record.'
    ],
    done: 'After publishing, select Open in Chronicle to view the record.',
    notes: ['You can export with open findings. The builder marks those files as drafts.']
  },
  admin: {
    name: 'Admin',
    lead: 'Manage scenario types and document templates.',
    steps: [
      'Use Scenario Types to edit and publish stages and questions.',
      'Use Libraries to publish document templates for selected scenario types.'
    ],
    done: 'New scenarios use the latest published type. Owners can review an update for an existing scenario. Published library templates appear on Finish.'
  }
};

/** The scenario's pages in the order a designer moves through them. */
export const JOURNEY: { page: PageId; title: string; line: string }[] = [
  { page: 'context', title: 'Context', line: 'Add notes, links or documents.' },
  { page: 'questions', title: 'Questions', line: 'Record decisions for each stage.' },
  { page: 'summary', title: 'Summary', line: 'Edit the executive summary.' },
  { page: 'log', title: 'Decision Log', line: 'Review changes and their authors.' },
  { page: 'topology', title: 'Topology', line: 'Build the range.' },
  { page: 'finish', title: 'Finish', line: 'Download files or publish to Chronicle.' }
];

/** The ledger's three marks, as the Stated, Proposed and Open concept draws them. */
export const MARKS: { mark: 'stated' | 'proposed' | 'open'; name: string; line: string }[] = [
  { mark: 'stated', name: 'Stated', line: 'You or another designer supplied the answer.' },
  { mark: 'proposed', name: 'Proposed', line: 'The agent chose the answer. Its explanation appears below it.' },
  { mark: 'open', name: 'Open', line: 'Not answered yet.' }
];

export const CONCEPTS: Concept[] = [
  {
    id: 'provenance',
    title: 'Stated, Proposed and Open',
    body: ['Each question shows whether a person or the agent supplied its answer.']
  },
  {
    id: 'stages',
    title: 'Stages',
    body: [
      'The scenario type groups related questions into stages.',
      'The agent can use answers from earlier stages when making recommendations.',
      'To open the next stage, answer every question in the current stage and save any required draft.'
    ]
  },
  {
    id: 'agent',
    title: 'Working with the Agent',
    body: [
      'The agent asks one question at a time and explains its recommended answer.',
      'Choose a recommendation, reply in your own words or answer a row on the Questions page.',
      'If the agent proposes a change to a stated answer, the scenario owner can apply or discard it.'
    ]
  },
  {
    id: 'handover',
    title: 'Handing Over',
    body: [
      'Press You decide on a question card and the agent answers that one question.',
      'Type “you decide” in the panel to delegate all remaining questions across all stages. The agent marks its answers as Proposed.',
      'Select Take back control to stop delegation. You can edit the answers already recorded.'
    ]
  },
  {
    id: 'sources',
    title: 'Sources',
    body: [
      'On the Context page, add notes, links or documents. The builder stores their text.',
      'The agent cites the sources it uses in a recommendation.'
    ]
  },
  {
    id: 'drafts',
    title: 'Drafts',
    body: [
      'Some stages require a written draft, such as an executive summary. The agent can draft it from your answers.',
      'Review and save the draft before closing its stage.'
    ]
  },
  {
    id: 'findings',
    title: 'Findings',
    body: [
      'A finding identifies an issue such as an unanswered question or a missing draft.',
      'You can export with open findings. The builder marks those documents as drafts.'
    ]
  },
  {
    id: 'validation',
    title: 'Why We Validate',
    body: ['The validator checks the scenario before it runs.'],
    points: [
      { lead: 'Review agent decisions.', body: 'The validator checks names, times, triggers and IDs. A reviewer decides whether the scenario makes sense.' },
      { lead: 'Catch errors during design.', body: 'A finding identifies where a trigger is broken or a host is missing.' },
      { lead: 'Correct errors.', body: 'The agent can use a finding’s location and suggested fix to correct its draft.' },
      { lead: 'Check outside contributions.', body: 'The same rules apply to scenarios from designers, agents and other builders.' },
      { lead: 'Validate offline.', body: 'The checks run without an AI model, including on an air-gapped range.' }
    ]
  },
  {
    id: 'visibility',
    title: 'Document audiences',
    body: [
      'Questions about restricted material, such as the ground truth, are marked “(exercise control only)”.',
      'Each document template has an audience. Participant documents omit exercise-control and designer-only material.'
    ]
  },
  {
    id: 'output',
    title: 'Exports',
    body: [
      'Scenario JSON contains the answers in a format other systems can read.',
      'Template libraries provide documents such as exercise plans, participant read-ins and facilitator guides.',
      'Apps can read the JSON through the pull API. On Finish, you can download it or publish the World and Intent to Chronicle.'
    ]
  }
];

export const GLOSSARY: Term[] = [
  { id: 'aar', term: 'After-Action Review', definition: 'The discussion after play about what happened, why, and what to change.', where: 'Questions about endings and outputs.', match: ['after-action review', 'AAR'] },
  { id: 'adjudication', term: 'Adjudication', definition: 'The process an assigned controller uses to decide the outcome of a participant’s action.', where: 'A question in the rules of play.', match: ['adjudication'] },
  { id: 'agent', term: 'Agent', definition: 'The assistant in the right-hand panel. It asks the questions, recommends answers, and can decide for you when asked.', where: 'The panel on the right of every scenario page.' },
  { id: 'attack', term: 'ATT&CK', definition: 'MITRE’s public catalogue of real attacker techniques, each with an ID such as T1566 (phishing).', where: 'Questions about the adversary.', match: ['ATT&CK'] },
  { id: 'decision-log', term: 'Decision Log', definition: 'The history of every answer, draft and proposal: who made it, when, and why.', where: 'The Decision Log page.' },
  { id: 'decision-point', term: 'Decision Point', definition: 'A point in the scenario when participants must choose how to respond.', where: 'Questions about events and decisions.', match: ['decision points', 'decision point'] },
  { id: 'designer', term: 'Designer', definition: 'The person who builds the scenario. Only designers can see material marked designer-only.', where: 'Document audiences on Finish.' },
  { id: 'document-template', term: 'Document Template', definition: 'A layout that turns the scenario into a document for one audience, such as a participant read-in.', where: 'Finish, under Document Templates.' },
  { id: 'draft', term: 'Draft', definition: 'A written section of the scenario, such as the executive summary. You or the agent can write it.', where: 'The Summary page; a stage marked Draft due.' },
  { id: 'enclave', term: 'Enclave', definition: 'The part of a range built for this one scenario: its sites, network segments and hosts.', where: 'The Topology and GraySpace pages; network questions.', match: ['enclaves', 'enclave'] },
  { id: 'escalation-ceiling', term: 'Escalation Ceiling', definition: 'The most severe situation the exercise allows. It sets a boundary for controllers as events unfold.', where: 'A stage title and a question.', match: ['escalation ceiling'] },
  { id: 'escalation-steps', term: 'Escalation Steps', definition: 'How the situation gets worse, step by step, up to the ceiling. One step is marked as the last the team can still recover from.', where: 'A question about events.', match: ['escalation steps'] },
  { id: 'executive-summary', term: 'Executive Summary', definition: 'A short account of the whole scenario for a sponsor or senior reader.', where: 'The Summary page.', match: ['executive summary'] },
  { id: 'excon', term: 'Exercise Control', definition: 'The people who run the exercise, introduce events and rule on participant actions. Also called ExCon or controllers.', where: 'Questions marked “(exercise control only)”.', match: ['exercise control', 'ExCon', 'controllers'] },
  { id: 'finding', term: 'Finding', definition: 'An issue to review, such as an unanswered question. Exports with open findings are marked as drafts.', where: 'The line above a stage’s questions; Finish.' },
  { id: 'fog', term: 'Fog', definition: 'Information withheld from participants so they practice making decisions with incomplete information.', where: 'A question in the rules of play.', match: ['fog'] },
  { id: 'gate', term: 'Gate', definition: 'The check for unanswered questions and missing drafts before a stage can close.', where: 'The line above a stage’s questions.' },
  { id: 'ground-truth', term: 'Ground Truth', definition: 'What is really happening in the scenario, known only to exercise control. Later checks catch contradictions against it.', where: 'A stage title and a question.', match: ['ground truth', 'ground-truth'] },
  { id: 'handover', term: 'Handing Over', definition: 'Letting the agent decide: one question with You decide on its card, or everything left by typing “you decide”.', where: 'The agent panel.' },
  { id: 'inject', term: 'Inject', definition: 'A message or event that exercise control introduces for participants to respond to.', where: 'Questions about key events, information and the network.', match: ['injects', 'inject'] },
  { id: 'objective', term: 'Objective', definition: 'What the exercise trains, starting with a verb: “Coordinate…”, “Decide…”. Each one needs a way to tell whether it was met.', where: 'Objectives questions.', match: ['objectives', 'objective'] },
  { id: 'participant', term: 'Participant', definition: 'Someone who plays the exercise. Participants never see exercise-control material.', where: 'Many questions; document audiences on Finish.' },
  { id: 'plugin', term: 'Plugin', definition: 'A connection to another app. An admin sets the app’s API URL on the Finish page.', where: 'Finish, under Plugins.' },
  { id: 'proposed', term: 'Proposed', definition: 'An answer chosen by the agent, shown with a violet dot and its reason. You can edit it.', where: 'Every question row and the Decision Log.' },
  { id: 'pull-api', term: 'Pull API', definition: 'The API that gives other systems the current scenario in JSON format.', where: 'Finish, under Export.' },
  { id: 'range', term: 'Range', definition: 'A built network of real or virtual systems where a team trains hands-on.', where: 'Small unit scenarios; the Topology page.', match: ['live range'] },
  { id: 'recommended-answer', term: 'Recommended Answer', definition: 'An answer suggested by the scenario type or the agent.', where: 'Question rows and the agent’s question cards.' },
  { id: 'red-herring', term: 'Red Herring', definition: 'A deliberate false lead. At most one per exercise, clearly marked in the ground truth.', where: 'A question about the premise.', match: ['red herring'] },
  { id: 'rules-of-play', term: 'Rules of Play', definition: 'How the exercise runs: pacing, the scenario clock, who adjudicates, and how much participants see.', where: 'A stage title.', match: ['rules of play'] },
  { id: 'scenario', term: 'Scenario', definition: 'The design for a training event, exercise or experiment, including its answers and drafts.', where: 'Everywhere.' },
  { id: 'scenario-json', term: 'Scenario JSON', definition: 'A machine-readable export of the scenario.', where: 'Finish, under Export.' },
  { id: 'scenario-type', term: 'Scenario Type', definition: 'The kind of exercise, such as a tabletop or a small unit team exercise. It sets the stages and questions, and can’t be changed once a scenario is made.', where: 'The Scenarios page; Admin.' },
  { id: 'sensitivity-review', term: 'Sensitivity Review', definition: 'A review of names, places and terms for unintended references, misleading details or offensive language.', where: 'A stage title.', match: ['sensitivity review'] },
  { id: 'simulated-users', term: 'Simulated Users', definition: 'Software characters (NPCs) that act like real people on the range or in the story: they send email, post and browse.', where: 'Population and network questions.', match: ['simulated users', 'NPCs'] },
  { id: 'source', term: 'Source', definition: 'Text you give the agent on Context: pasted notes, a link or a document.', where: 'The Context page.' },
  { id: 'stage', term: 'Stage', definition: 'A group of related questions. Stages open in order, each once the one before it is answered.', where: 'The rail on the left of Questions.' },
  { id: 'stated', term: 'Stated', definition: 'An answer supplied by a person, either directly or through the agent. A blue dot marks it.', where: 'Every question row and the Decision Log.' },
  { id: 'success-criteria', term: 'Success Criteria', definition: 'How you will tell each objective was met. An objective without one can’t be assessed.', where: 'Objectives questions.', match: ['success criteria'] },
  { id: 'ttx', term: 'Tabletop Exercise (TTX)', definition: 'A discussion-based exercise: leaders talk through a developing situation and make decisions, with no live systems.', where: 'Scenario types.', match: ['tabletop exercise', 'TTX'] },
  { id: 'tcs', term: 'Tasks, Conditions and Standards', definition: 'A training format that identifies the task, the conditions under which the team performs it, and the standard for success.', where: 'A stage title in small unit scenarios.', match: ['tasks, conditions and standards'] },
  { id: 'teaching-points', term: 'Teaching Points', definition: 'Lessons participants should draw from the exercise. Facilitators use them to guide discussion.', where: 'A stage title and a question.', match: ['teaching points'] },
  { id: 'template-library', term: 'Template Library', definition: 'A set of document templates for a scenario type, managed in Admin.', where: 'Finish; Admin › Libraries.' },
  { id: 'topology', term: 'Topology', definition: 'The network a scenario needs and where it is built. Only scenario types with a range have a Topology page.', where: 'The Topology page.' },
  { id: 'visibility', term: 'Visibility', definition: 'Who may see an answer: participants, exercise control, or designers only.', where: 'Questions marked “(exercise control only)”; document audiences.' }
];

export const TOUR: TourStop[] = [
  { target: 'pages', title: 'Scenario pages', body: 'Use these links to switch between your sources, questions, summary and exports.' },
  { target: 'rail', title: 'Stages', body: 'The bars show how many questions you’ve answered in each stage.' },
  { target: 'ledger', title: 'Question status', body: 'Select Answer to enter a decision. A blue dot marks your answer or another designer’s. A violet dot marks an answer chosen by the agent. A ring marks an unanswered question.' },
  { target: 'gate', title: 'Stage progress', body: 'Check here for unanswered questions and missing drafts. When the stage is complete, you can open the next one.' },
  { target: 'agent', title: 'The Agent', body: 'Ask for a recommendation or type “you decide” to delegate all remaining questions.' },
  { target: 'guide', title: 'Guide', body: 'Open the Guide here or press ? for page instructions and the glossary.' }
];

export const WELCOME_STEPS: { title: string; body: string }[] = [
  { title: 'Add sources', body: 'Give the agent notes, links or documents to consult.' },
  { title: 'Answer questions', body: 'Work through the questions in each stage.' },
  { title: 'Review the scenario', body: 'Edit the summary and check the Decision Log.' },
  { title: 'Export the scenario', body: 'Download files or publish to Chronicle.' }
];
