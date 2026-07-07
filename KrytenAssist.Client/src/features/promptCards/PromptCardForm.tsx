import { useState } from 'react';
import type { FormEvent } from 'react';
import { createPromptCard } from '../../api/promptCardsApi';

interface PromptCardFormProps {
    onPromptCardCreated: () => Promise<void>;
}

function PromptCardForm({ onPromptCardCreated }: PromptCardFormProps) {
    const [title, setTitle] = useState('');
    const [category, setCategory] = useState('');
    const [description, setDescription] = useState('');
    const [promptText, setPromptText] = useState('');
    const [tags, setTags] = useState('');
    const [error, setError] = useState<string | null>(null);
    const [successMessage, setSuccessMessage] = useState<string | null>(null);

    async function handleSubmit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        setError(null);
        setSuccessMessage(null);

        const request = {
            title,
            category,
            description,
            promptText,
            tags: tags
                .split(',')
                .map(tag => tag.trim())
                .filter(tag => tag.length > 0),
        };

        try {

            await createPromptCard(request);

            setSuccessMessage('Prompt Card created successfully.');
            await onPromptCardCreated();
            setTitle('');
            setCategory('');
            setDescription('');
            setPromptText('');
            setTags('');
        } catch {
            setError('Unable to create Prompt Card. Please check the API is running and try again.');
        }
    }

    return (
        <section>
            <h2>Create Prompt Card</h2>

            {error && <p className="form-error">{error}</p>}
            {successMessage && <p className="form-success">{successMessage}</p>}
            <form onSubmit={handleSubmit}>
                <label>
                    Title
                    <input
                        type="text"
                        value={title}
                        onChange={event => setTitle(event.target.value)}
                    />
                </label>

                <label>
                    Category
                    <input
                        type="text"
                        value={category}
                        onChange={event => setCategory(event.target.value)}
                    />
                </label>

                <label>
                    Description
                    <textarea
                        value={description}
                        onChange={event => setDescription(event.target.value)}
                    />
                </label>

                <label>
                    Prompt Text
                    <textarea
                        value={promptText}
                        onChange={event => setPromptText(event.target.value)}
                    />
                </label>

                <label>
                    Tags
                    <input
                        type="text"
                        placeholder="react, forms, prompt-020"
                        value={tags}
                        onChange={event => setTags(event.target.value)}
                    />
                </label>

                <button type="submit">Create Prompt Card</button>
            </form>
        </section>
    );
}

export default PromptCardForm;
