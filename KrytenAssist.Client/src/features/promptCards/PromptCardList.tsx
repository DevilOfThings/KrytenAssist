import { useEffect, useState } from 'react';
import { getPromptCards } from '../../api/promptCardsApi';
import type { PromptCard } from './PromptCard';
import PromptCardItem from './PromptCardItem';

function PromptCardList() {
    const [promptCards, setPromptCards] = useState<PromptCard[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string>();

    useEffect(() => {
        async function loadPromptCards() {
            try {
                const data = await getPromptCards();
                setPromptCards(data);
            } catch {
                setError('Unable to load Prompt Cards.');
            } finally {
                setLoading(false);
            }
        }

        void loadPromptCards();
    }, []);

    if (loading) {
        return <p>Loading...</p>;
    }

    if (error) {
        return <p>{error}</p>;
    }

    return (
        <section>
            <h2>Prompt Cards</h2>

            {promptCards.length === 0 && (
                <p>No Prompt Cards found.</p>
            )}

            <ul>
                {promptCards.map(card => (
                    <li key={card.id}>
                    <PromptCardItem promptCard={card} />
                    </li>
                ))}
            </ul>
        </section>
    );
}

export default PromptCardList;