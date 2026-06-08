import React from "react";
import CommentApi from "../api/CommentApi";
import type { GetCommentsRequest, Comment, AddCommentRequest } from "../interfaces/GlobalInterfaceExport";

interface CommentSectionProps {
    postId: number;
}

export default function CommentSection({ postId }: CommentSectionProps) {
    const [comments, setComments] = React.useState<Comment[]>([]);
    const [pageNumber, setPageNumber] = React.useState(1);
    const [loading, setLoading] = React.useState(false);
    const [hasMore, setHasMore] = React.useState(true);
    const [newComment, setNewComment] = React.useState("");

    const loaderRef = React.useRef<HTMLDivElement | null>(null);

    const loadComments = async () => {
        if (loading || !hasMore) return;
        setLoading(true);
        try {
            const req: GetCommentsRequest = { postId, pageNumber, pageSize: 10 };
            const res = await CommentApi.getComments(req);
            if (!res.Success) throw new Error(res.Message || "Unable to fetch comments");
            const list: Comment[] = res.Data ?? [];
            if (list.length === 0) setHasMore(false);
            setComments(prev => [...prev, ...list]);
        } catch (ex) {
            console.error(ex);
        } finally {
            setLoading(false);
        }
    };

    React.useEffect(() => { loadComments(); }, [pageNumber]);

    React.useEffect(() => {
        if (!loaderRef.current || !hasMore) return;
        const observer = new IntersectionObserver((entries) => {
            if (entries[0].isIntersecting) setPageNumber(prev => prev + 1);
        });
        observer.observe(loaderRef.current);
        return () => observer.disconnect();
    }, [hasMore]);

    const addComment = async () => {
        if (!newComment.trim()) return;
        try {
            const req: AddCommentRequest = { postId, content: newComment };
            const res = await CommentApi.addComment(req);
            if (!res.Success) throw new Error(res.Message || "Unable to add comment");
            setComments(prev => [res.Data as Comment, ...prev]);
            setNewComment("");
        } catch (ex) {
            console.error(ex);
        }
    };

    return (
        <div className="p-4 max-h-[70vh] overflow-y-auto">
            <h3 className="text-base font-semibold mb-3" style={{ color: "var(--color-text)" }}>
                Comments
            </h3>

            <div className="space-y-3 mb-4">
                {comments.map((c, i) => (
                    <div key={i} className="text-sm" style={{ color: "var(--color-text)" }}>
                        <span className="font-medium">User {c.userId}: </span>
                        <span>{c.content}</span>
                        <div className="text-xs mt-0.5" style={{ color: "var(--color-muted)" }}>
                            {c.likes} likes
                        </div>
                    </div>
                ))}
            </div>

            <div ref={loaderRef} />
            {loading && <p className="text-xs text-center py-2" style={{ color: "var(--color-muted)" }}>Loading…</p>}

            <div className="flex gap-2 mt-3">
                <input
                    className="flex-1 border rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500"
                    style={{
                        background: "var(--color-surface)",
                        borderColor: "var(--color-border)",
                        color: "var(--color-text)"
                    }}
                    placeholder="Write a comment..."
                    value={newComment}
                    onChange={(e) => setNewComment(e.target.value)}
                    onKeyDown={(e) => e.key === "Enter" && addComment()}
                />
                <button
                    onClick={addComment}
                    className="px-4 py-2 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700 transition-colors"
                >
                    Send
                </button>
            </div>
        </div>
    );
}
