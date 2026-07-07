import { useEffect, useState, useMemo } from 'react';
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

    const [searchTerm, setSearchTerm] = useState('');
    const [selectedCategory, setSelectedCategory] = useState('');

    const categories = useMemo(() => {
        return Array.from(new Set(promptCards.map(card => card.category))).sort();
    }, [promptCards]);

    const filteredPromptCards = useMemo(() => {
        const search = searchTerm.trim().toLowerCase();

        return promptCards.filter(card => {
            const matchesSearch = search.length === 0 ||
                card.title.toLowerCase().includes(search) ||
                (card.description?.toLowerCase().includes(search) ?? false);

            const matchesCategory = selectedCategory.length === 0 ||
                card.category === selectedCategory;

            return matchesSearch && matchesCategory;
        });
    }, [promptCards, searchTerm, selectedCategory]);

    if (loading) {
        return (
            <section>
                <h2>Prompt Browser</h2>
                <p>Loading prompt cards...</p>
            </section>
        );
    }

    if (error) {
        return (
            <section>
                <h2>Prompt Browser</h2>
                <p>{error}</p>
            </section>
        );
    }

    return (
        <section>
            <header>
                <h2>Prompt Browser</h2>
                <p>{promptCards.length} prompt card{promptCards.length === 1 ? '' : 's'} found.</p>
            </header>

            <div className="filter-bar">
                <input
                    type="search"
                    placeholder="Search prompt cards..."
                    value={searchTerm}
                    onChange={event => setSearchTerm(event.target.value)}
                />

                <select
                    value={selectedCategory}
                    onChange={event => setSelectedCategory(event.target.value)}
                >
                    <option value="">All Categories</option>
                    {categories.map(category => (
                        <option key={category} value={category}>
                            {category}
                        </option>
                    ))}
                </select>
            </div>

            {promptCards.length === 0 && (
                <p>No Prompt Cards found.</p>
            )}

            {promptCards.length > 0 && filteredPromptCards.length === 0 && (
                <p>No prompt cards match your search or filter.</p>
            )}

            {filteredPromptCards.length > 0 && (
                <ul>
                    {filteredPromptCards.map(card => (
                        <li key={card.id}>
                            <PromptCardItem promptCard={card} />
                        </li>
                    ))}
                </ul>
            )}
        </section>
    );
}

export default PromptCardList;