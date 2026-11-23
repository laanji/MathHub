export interface Account {
  id: string;
  email: string;
  displayName: string;
  role: "Student" | "Parent" | "Administrator";
  birthDate: string;
  mathScore: number | null;
  rank: string | null;
  subRank: string | null;
}
export interface Context {
  id: string;
  name: string;
  initialTheta: number;
}
export interface Test {
  id: string;
  context: string;
  status: string;
  version: number;
  minimumQuestions: number;
  maximumQuestions: number;
  question: {
    id: string;
    text: string;
    topic: string;
    options: string[];
  } | null;
  result: {
    mathScore: number;
    rank: string;
    subRank: string;
    correct: number;
    total: number;
    completedAt: string;
  } | null;
}
export interface Course {
  id: string;
  title: string;
  description: string;
  context: string;
  pricePerLesson: number;
  slots: string[];
}
export interface Enrollment {
  id: string;
  courseId: string;
  courseTitle: string;
  groupId: string;
  slot: string;
  status: string;
  groupState: string;
  activeCount: number;
  occupiedSeats: number;
  scoreAtEntry: number;
  priceSnapshot: number;
  reservedUntil: string;
}
export interface Child {
  id: string;
  displayName: string;
  confirmed: boolean;
  mathScore: number | null;
}
export interface Question {
  id: string;
  context: string;
  text: string;
  topic: string;
  optionsJson: string;
  correctAnswer: string;
  difficulty: number;
  version: number;
  active: boolean;
}
