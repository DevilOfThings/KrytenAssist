import type { PromptCard } from './PromptCard';

interface PromptCardItemProps {
    promptCard: PromptCard;
}

function PromptCardItem({ promptCard }: PromptCardItemProps) {
    return (
        <article>
            <h3>{promptCard.title}</h3>

            <p>
                <strong>Category:</strong> {promptCard.category}
            </p>

            {promptCard.description && (
                <p>{promptCard.description}</p>
            )}
        </article>
    );
}

export default PromptCardItem;