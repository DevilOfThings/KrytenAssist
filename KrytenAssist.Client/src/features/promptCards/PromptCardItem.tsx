import type { PromptCard } from './PromptCard';

interface PromptCardItemProps {
    promptCard: PromptCard;
}

function PromptCardItem({ promptCard }: PromptCardItemProps) {
    return (
        <article>
            <header>
                <h3>{promptCard.title}</h3>
                <p>
                    <strong>Category:</strong> {promptCard.category}
                </p>
            </header>

            {promptCard.description && (
                <p>{promptCard.description}</p>
            )}

            <p>
                <strong>Prompt:</strong> {promptCard.promptText}
            </p>

            {promptCard.tags.length > 0 && (
                <p>
                    <strong>Tags:</strong> {promptCard.tags.join(', ')}
                </p>
            )}
        </article>
    );
}

export default PromptCardItem;