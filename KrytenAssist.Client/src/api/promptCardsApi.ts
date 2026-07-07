import { get } from './apiClient';
import type { PromptCard } from '../features/promptCards/PromptCard';

export function getPromptCards(): Promise<PromptCard[]> {
    return get<PromptCard[]>('/promptcards');
}