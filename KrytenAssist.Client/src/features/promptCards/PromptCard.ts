export interface PromptCard {
    id: string;
    title: string;
    category: string;
    description?: string;
    promptText: string;
    tags: string[];
}