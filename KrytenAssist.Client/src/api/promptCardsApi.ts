import { get, post } from './apiClient';
import type { PromptCard } from '../features/promptCards/PromptCard';


export interface CreatePromptCardRequest {
    title: string;
    category: string;
    description: string;
    promptText: string;
    tags: string[];
}

export function createPromptCard(request: CreatePromptCardRequest): Promise<PromptCard> {
    return post<PromptCard, CreatePromptCardRequest>('/promptcards', request);
}


export function getPromptCards(): Promise<PromptCard[]> {
    return get<PromptCard[]>('/promptcards');
}